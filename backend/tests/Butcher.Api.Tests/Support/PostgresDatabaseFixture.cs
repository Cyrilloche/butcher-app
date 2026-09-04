using Butcher.Api.Domain.Entities;
using Butcher.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Butcher.Api.Tests.Support;

public class PostgresDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("butcher_test")
        .WithUsername("butcher_test")
        .WithPassword("butcher_test")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }

    public async Task ResetAsync()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                stock_movement,
                sale,
                stock_unit,
                production_batch,
                product,
                unit_of_measure,
                customer,
                refresh_token,
                app_user
            RESTART IDENTITY CASCADE
            """);
    }

    public static UserManager<AppUser> CreateUserManager(AppDbContext dbContext)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(dbContext);
        services.AddIdentityCore<AppUser>().AddEntityFrameworkStores<AppDbContext>();
        return services.BuildServiceProvider().GetRequiredService<UserManager<AppUser>>();
    }

    public static IConfiguration CreateJwtConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "test-signing-key-for-unit-tests-at-least-32-chars",
                ["Jwt:Issuer"] = "butcher-api-tests",
                ["Jwt:Audience"] = "butcher-app-tests",
                ["Jwt:AccessTokenLifetimeMinutes"] = "15",
                ["Jwt:RefreshTokenLifetimeDays"] = "30",
            })
            .Build();
}
