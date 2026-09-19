using System.Diagnostics.CodeAnalysis;
using Dapper;
using Evently.Common.Application.Caching;
using Evently.Common.Application.Clock;
using Evently.Common.Application.Data;
using Evently.Common.Application.EventBus;
using Evently.Common.Infrastructure.Authentication;
using Evently.Common.Infrastructure.Authorization;
using Evently.Common.Infrastructure.Caching;
using Evently.Common.Infrastructure.Clock;
using Evently.Common.Infrastructure.Data;
using Evently.Common.Infrastructure.Outbox;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace Evently.Common.Infrastructure;

[SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
public static class InfrastructureConfiguration
{
    public static void AddInfrastructure(this IHostApplicationBuilder builder,
        string serviceName,
        Action<IRegistrationConfigurator, string>[] moduleConfigureConsumers)
    {
        builder.Services.AddAuthenticationInternal();

        builder.Services.AddAuthorizationInternal();

        builder.AddNpgsqlDataSource("evently-db");

        builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
        SqlMapper.AddTypeHandler(new GenericArrayHandler<string>());

        builder.Services.AddQuartz(configurator =>
        {
            var scheduler = Guid.NewGuid();
            configurator.SchedulerId = $"default-id-{scheduler}";
            configurator.SchedulerName = $"default-name-{scheduler}";
        });
        builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        builder.Services.TryAddSingleton<InsertOutboxMessagesInterceptor>();

        builder.Services.TryAddSingleton<IDateTimeProvider, DateTimeProvider>();

        // try
        // {
        //     IConnectionMultiplexer connectionMultiplexer =
        //         ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("evently-redis")!);
        //     builder.Services.TryAddSingleton(connectionMultiplexer);
        //
        //     builder.Services.AddStackExchangeRedisCache(options =>
        //         options.ConnectionMultiplexerFactory = () => Task.FromResult(connectionMultiplexer));
        // }
        // catch
        // {
        //     builder.Services.AddDistributedMemoryCache();
        // }
        builder.AddRedisDistributedCache("evently-redis");


        builder.Services.TryAddSingleton<ICacheService, CacheService>();
        builder.Services.TryAddSingleton<IEventBus, EventBus.EventBus>();

        // Manual MassTransit + RabbitMQ setup, replaced by the Aspire-integrated
        // AddMassTransitRabbitMq call below, which resolves the connection string
        // from the "evently-queue" RabbitMQ resource defined in the AppHost.
        // builder.Services.AddMassTransit(configure =>
        // {
        //     string instanceId = serviceName.ToLowerInvariant().Replace('.', '-');
        //     foreach (Action<IRegistrationConfigurator, string> configureConsumers in moduleConfigureConsumers)
        //     {
        //         configureConsumers(configure, instanceId);
        //     }
        //
        //     configure.SetKebabCaseEndpointNameFormatter();
        //
        //     configure.UsingRabbitMq((context, cfg) =>
        //     {
        //         cfg.Host(new Uri(rabbitMqSettings.Host), h =>
        //         {
        //             h.Username(rabbitMqSettings.Username);
        //             h.Password(rabbitMqSettings.Password);
        //         });
        //
        //         cfg.ConfigureEndpoints(context);
        //     });
        // });

        string instanceId = serviceName.ToLowerInvariant().Replace('.', '-');

        builder.AddMassTransitRabbitMq(
            "evently-queue",
            massTransitConfiguration: configure =>
            {
                foreach (Action<IRegistrationConfigurator, string> configureConsumer in moduleConfigureConsumers)
                {
                    configureConsumer(configure, instanceId);
                }
            });
    }
}
