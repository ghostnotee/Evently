using Evently.Modules.Users.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Keycloak;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Evently.IntegrationTests.Abstractions;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:latest")
        .WithDatabase("evently")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly KeycloakContainer _keycloakContainer = new KeycloakBuilder("quay.io/keycloak/keycloak:latest")
        .WithResourceMapping(
            new FileInfo("evently-realm-export.json"),
            new FileInfo("/opt/keycloak/data/import/realm.json"))
        .WithCommand("--import-realm")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:latest")
        .Build();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _redisContainer.StartAsync();
        await _keycloakContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.StopAsync();
        await _keycloakContainer.StopAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        string dbConnectionString = _dbContainer.GetConnectionString();
        string redisConnectionString = _redisContainer.GetConnectionString();

        Environment.SetEnvironmentVariable("ConnectionStrings:evently-db", dbConnectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings:evently-redis", redisConnectionString);

        string keycloakAddress = _keycloakContainer.GetBaseAddress();
        string keyCloakRealmUrl = $"{keycloakAddress}realms/evently";

        Environment.SetEnvironmentVariable(
            "Authentication:MetadataAddress",
            $"{keyCloakRealmUrl}/.well-known/openid-configuration");
        Environment.SetEnvironmentVariable(
            "Authentication:TokenValidationParameters:ValidIssuer",
            keyCloakRealmUrl);

        builder.ConfigureTestServices(services =>
        {
            services.Configure<KeyCloakOptions>(o =>
            {
                o.AdminUrl = $"{keycloakAddress}admin/realms/evently/";
                o.TokenUrl = $"{keyCloakRealmUrl}/protocol/openid-connect/token";
            });
        });
    }
}
