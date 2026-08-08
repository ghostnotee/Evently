using Evently.Common.Application.Authorization;
using Evently.Common.Infrastructure.Interceptors;
using Evently.Common.Presentation.Endpoints;
using Evently.Modules.Users.Application.Abstractions.Data;
using Evently.Modules.Users.Application.Abstractions.Identity;
using Evently.Modules.Users.Domain.Users;
using Evently.Modules.Users.Infrastructure.Authorization;
using Evently.Modules.Users.Infrastructure.Database;
using Evently.Modules.Users.Infrastructure.Identity;
using Evently.Modules.Users.Infrastructure.Users;
using Evently.Modules.Users.Presentation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Evently.Modules.Users.Infrastructure;

public static class UsersModule
{
    extension(IHostApplicationBuilder builder)
    {
        public void AddUsersModule()
        {
            builder.AddInfrastructure();
            builder.Services.AddEndpoints(AssemblyReference.Assembly);
        }

        private void AddInfrastructure()
        {
            builder.Services.AddScoped<IPermissionService, PermissionService>();
            builder.Services.Configure<KeyCloakOptions>(builder.Configuration.GetSection("Users:KeyCloak"));
            builder.Services.AddTransient<KeyCloakAuthDelegatingHandler>();
            builder.Services
                .AddHttpClient<KeyCloakClient>((serviceProvider, httpClient) =>
                {
                    KeyCloakOptions keyCloakOptions = serviceProvider.GetRequiredService<IOptions<KeyCloakOptions>>().Value;
                    httpClient.BaseAddress = new Uri(keyCloakOptions.AdminUrl);
                })
                .AddHttpMessageHandler<KeyCloakAuthDelegatingHandler>();

            builder.Services.AddTransient<IIdentityProviderService, IdentityProviderService>();

            string connectionString = builder.Configuration.GetConnectionString("evently-db")
                                      ?? throw new InvalidOperationException(
                                          "Connection string 'evently-db' was not found.");
            builder.Services.AddDbContext<UsersDbContext>((provider, optionsBuilder) =>
            {
                optionsBuilder.UseNpgsql(connectionString,
                        contextOptionsBuilder => contextOptionsBuilder
                            .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Users))
                    .UseSnakeCaseNamingConvention()
                    .AddInterceptors(provider.GetRequiredService<PublishDomainEventsInterceptor>());
            });

            builder.EnrichNpgsqlDbContext<UsersDbContext>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UsersDbContext>());
        }
    }
}
