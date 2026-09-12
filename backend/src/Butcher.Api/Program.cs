using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Butcher.Api.Application.Services;
using Butcher.Api.Common;
using Butcher.Api.Common.Authorization;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Butcher.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

// Commandes hors-ligne de gestion de compte, lancées dans le conteneur en prod :
// `create-user <email> <mot-de-passe>` et `set-password <email> <mot-de-passe>`.
// Les arguments positionnels ne sont pas passés au builder : le fournisseur de
// configuration ligne de commande les rejetterait.
var createUserCommand = args is ["create-user", ..];
var setPasswordCommand = args is ["set-password", ..];

var builder = WebApplication.CreateBuilder(createUserCommand || setPasswordCommand ? [] : args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Jeton d'accès obtenu via POST /api/auth/login.",
        };
        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
        });
        return Task.CompletedTask;
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
        .UseSnakeCaseNamingConvention());

builder.Services
    .AddIdentityCore<AppUser>(IdentityPolicy.Configure)
    .AddSaloirPasswordRules()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddLoginRateLimiter();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });

// Fail-closed (ADR-009) et compte relu en base à chaque requête (ADR-011) : toute route exige un compte
// authentifié et actif, sauf [AllowAnonymous] ; les gestes réservés exigent en plus l'administrateur.
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = AuthorizationPolicies.ActiveAccount;
    options.FallbackPolicy = AuthorizationPolicies.ActiveAccount;
    options.AddPolicy(AuthorizationPolicies.AdminOnly, AuthorizationPolicies.Admin);
});
builder.Services.AddScoped<IAuthorizationHandler, AccountAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AccountAuthorizationResultHandler>();

var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"];
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigin!).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentAccount, HttpCurrentAccount>();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductionBatchService, ProductionBatchService>();
builder.Services.AddScoped<IStockUnitService, StockUnitService>();
builder.Services.AddScoped<IStockMovementService, StockMovementService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISaleService, SaleService>();

var app = builder.Build();

if (createUserCommand)
{
    return await CreateUserAsync(app, args);
}

if (setPasswordCommand)
{
    return await SetPasswordAsync(app, args);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await MigrateDatabaseAsync(app);
await SeedAdminUserAsync(app);

app.Run();

return 0;

static async Task MigrateDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

static async Task SeedAdminUserAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

    if (await userManager.Users.AnyAsync())
    {
        return;
    }

    var email = app.Configuration["Seed:AdminEmail"];
    var password = app.Configuration["Seed:AdminPassword"];

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        app.Logger.LogWarning(
            "Aucun app_user n'existe et Seed:AdminEmail/Seed:AdminPassword ne sont pas configurés — connexion impossible.");
        return;
    }

    // Le premier compte d'une base vierge est forcément administrateur : sans lui, personne ne
    // pourrait créer les autres comptes (ADR-011).
    var user = new AppUser
    {
        UserName = email,
        Email = email,
        DisplayName = DisplayNameFromEmail(email),
        Role = AccountRole.Admin,
    };
    var result = await userManager.CreateAsync(user, password);

    if (!result.Succeeded)
    {
        throw new InvalidOperationException(
            $"Échec de la création du compte seedé : {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }

    app.Logger.LogInformation("Compte administrateur seedé pour {Email}", email);
}

static async Task<int> CreateUserAsync(WebApplication app, string[] args)
{
    if (args.Length != 3)
    {
        await Console.Error.WriteLineAsync("Usage : create-user <email> <mot-de-passe>");
        return 1;
    }

    var email = args[1];
    var password = args[2];

    using var scope = app.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

    if (await userManager.FindByEmailAsync(email) is not null)
    {
        await Console.Error.WriteLineAsync($"Un compte existe déjà pour {email}.");
        return 1;
    }

    // Compte utilisateur par défaut : les administrateurs se créent ou se promeuvent depuis
    // l'interface, par un administrateur existant (ADR-011).
    var user = new AppUser
    {
        UserName = email,
        Email = email,
        DisplayName = DisplayNameFromEmail(email),
        Role = AccountRole.User,
    };
    var result = await userManager.CreateAsync(user, password);

    if (!result.Succeeded)
    {
        await Console.Error.WriteLineAsync(
            $"Échec de la création du compte : {string.Join(", ", result.Errors.Select(e => e.Description))}");
        return 1;
    }

    Console.WriteLine($"Compte utilisateur créé pour {email}.");
    return 0;
}

// Nom affiché provisoire, corrigeable ensuite depuis l'écran des comptes. Même règle que la reprise
// des comptes existants dans la migration AddAccountRoles.
static string DisplayNameFromEmail(string email) => email.Split('@')[0];

// Remplace le mot de passe d'un compte existant. La politique de mot de passe ne s'applique qu'à
// l'écriture : sans cette commande, un compte créé avant son durcissement garderait son ancien mot
// de passe. Le changement révoque aussi toutes les sessions ouvertes du compte.
static async Task<int> SetPasswordAsync(WebApplication app, string[] args)
{
    if (args.Length != 3)
    {
        await Console.Error.WriteLineAsync("Usage : set-password <email> <mot-de-passe>");
        return 1;
    }

    var email = args[1];
    var password = args[2];

    using var scope = app.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var user = await userManager.FindByEmailAsync(email);
    if (user is null)
    {
        await Console.Error.WriteLineAsync($"Aucun compte pour {email}.");
        return 1;
    }

    var token = await userManager.GeneratePasswordResetTokenAsync(user);
    var result = await userManager.ResetPasswordAsync(user, token, password);

    if (!result.Succeeded)
    {
        await Console.Error.WriteLineAsync(
            $"Échec du changement de mot de passe : {string.Join(", ", result.Errors.Select(e => e.Description))}");
        return 1;
    }

    var now = DateTimeOffset.UtcNow;
    await dbContext.RefreshTokens
        .Where(t => t.UserId == user.Id && t.RevokedAt == null)
        .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));
    await userManager.ResetAccessFailedCountAsync(user);
    await userManager.SetLockoutEndDateAsync(user, null);

    Console.WriteLine($"Mot de passe changé pour {email}. Les sessions ouvertes sont révoquées.");
    return 0;
}
