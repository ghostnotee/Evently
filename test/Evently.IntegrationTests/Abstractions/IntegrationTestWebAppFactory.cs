using Evently.Modules.Users.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Keycloak;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace Evently.IntegrationTests.Abstractions;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18.6-bookworm")
        .WithDatabase("evently")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly KeycloakContainer _keycloakContainer = new KeycloakBuilder("quay.io/keycloak/keycloak:26.7")
        .WithResourceMapping(
            new FileInfo("evently-realm-export.json"),
            new FileInfo("/opt/keycloak/data/import/realm.json"))
        .WithCommand("--import-realm")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:8.10.2")
        .Build();
    
    private readonly RabbitMqContainer _rabbitMqContainer =
        new RabbitMqBuilder("rabbitmq:4.3.6-alpine").Build();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _redisContainer.StartAsync();
        await _keycloakContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _redisContainer.StopAsync();
        await _keycloakContainer.StopAsync();
        await _rabbitMqContainer.StopAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        string dbConnectionString = _dbContainer.GetConnectionString();
        string redisConnectionString = _redisContainer.GetConnectionString();
        string rabbitMqConnectionString = _rabbitMqContainer.GetConnectionString();

        Environment.SetEnvironmentVariable("ConnectionStrings:evently-db", dbConnectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings:evently-cache", redisConnectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings:evently-queue", rabbitMqConnectionString);

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
