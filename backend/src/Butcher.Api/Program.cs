using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Butcher.Api.Application.Services;
using Butcher.Api.Common;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

// Commande hors-ligne de création de compte (`create-user <email> <mot-de-passe>`),
// lancée dans le conteneur en prod. Les arguments positionnels ne sont pas passés
// au builder : le fournisseur de configuration ligne de commande les rejetterait.
var createUserCommand = args is ["create-user", ..];

var builder = WebApplication.CreateBuilder(createUserCommand ? [] : args);

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
    .AddIdentityCore<AppUser>(options => options.User.RequireUniqueEmail = true)
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

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

builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"];
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigin!).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
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

    var user = new AppUser { UserName = email, Email = email, CreatedAt = DateTimeOffset.UtcNow };
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

    var user = new AppUser { UserName = email, Email = email, CreatedAt = DateTimeOffset.UtcNow };
    var result = await userManager.CreateAsync(user, password);

    if (!result.Succeeded)
    {
        await Console.Error.WriteLineAsync(
            $"Échec de la création du compte : {string.Join(", ", result.Errors.Select(e => e.Description))}");
        return 1;
    }

    Console.WriteLine($"Compte créé pour {email}.");
    return 0;
}
