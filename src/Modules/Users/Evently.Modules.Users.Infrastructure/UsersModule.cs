using Evently.Common.Application.Authorization;
using Evently.Common.Application.EventBus;
using Evently.Common.Application.Messaging;
using Evently.Common.Infrastructure.Outbox;
using Evently.Common.Presentation.Endpoints;
using Evently.Modules.Users.Application.Abstractions.Data;
using Evently.Modules.Users.Application.Abstractions.Identity;
using Evently.Modules.Users.Domain.Users;
using Evently.Modules.Users.Infrastructure.Authorization;
using Evently.Modules.Users.Infrastructure.Database;
using Evently.Modules.Users.Infrastructure.Identity;
using Evently.Modules.Users.Infrastructure.Inbox;
using Evently.Modules.Users.Infrastructure.Outbox;
using Evently.Modules.Users.Infrastructure.Users;
using Evently.Modules.Users.Presentation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Evently.Modules.Users.Infrastructure;

public static class UsersModule
{
    extension(IHostApplicationBuilder builder)
    {
        public void AddUsersModule()
        {
            builder.Services.AddDomainEventHandlers();
            builder.Services.AddIntegrationEventHandlers();
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
                    .AddInterceptors(provider.GetRequiredService<InsertOutboxMessagesInterceptor>());
            });

            builder.EnrichNpgsqlDbContext<UsersDbContext>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UsersDbContext>());
            builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Users:Outbox"));

            builder.Services.ConfigureOptions<ConfigureProcessOutboxJob>();
        }
    }

    private static void AddDomainEventHandlers(this IServiceCollection services)
    {
        Type[] domainEventHandlers = Application.AssemblyReference.Assembly
            .GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IDomainEventHandler)))
            .ToArray();

        foreach (Type domainEventHandler in domainEventHandlers)
        {
            services.TryAddScoped(domainEventHandler);

            Type domainEvent = domainEventHandler
                .GetInterfaces()
                .Single(i => i.IsGenericType)
                .GetGenericArguments()
                .Single();

            Type closedIdempotentHandler = typeof(IdempotentDomainEventHandler<>).MakeGenericType(domainEvent);

            services.Decorate(domainEventHandler, closedIdempotentHandler);
        }
    }

    private static void AddIntegrationEventHandlers(this IServiceCollection services)
    {
        Type[] integrationEventHandlers = AssemblyReference.Assembly
            .GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IIntegrationEventHandler)))
            .ToArray();

        foreach (Type integrationEventHandler in integrationEventHandlers)
        {
            services.TryAddScoped(integrationEventHandler);

            Type integrationEvent = integrationEventHandler
                .GetInterfaces()
                .Single(i => i.IsGenericType)
                .GetGenericArguments()
                .Single();

            Type closedIdempotentHandler = typeof(IdempotentIntegrationEventHandler<>).MakeGenericType(integrationEvent);

            services.Decorate(integrationEventHandler, closedIdempotentHandler);
        }
    }
}
