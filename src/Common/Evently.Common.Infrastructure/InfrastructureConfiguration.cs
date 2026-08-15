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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Quartz;
using StackExchange.Redis;

namespace Evently.Common.Infrastructure;

public static class InfrastructureConfiguration
{
    public static void AddInfrastructure(this IHostApplicationBuilder builder,
        Action<IRegistrationConfigurator>[] moduleConfigureConsumers)
    {
        builder.Services.AddAuthenticationInternal();

        builder.Services.AddAuthorizationInternal();

        builder.AddNpgsqlDataSource("evently-db");

        builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
        SqlMapper.AddTypeHandler(new GenericArrayHandler<string>());

        builder.Services.AddQuartz();
        builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        builder.Services.TryAddSingleton<InsertOutboxMessagesInterceptor>();

        builder.Services.TryAddSingleton<IDateTimeProvider, DateTimeProvider>();

        try
        {
            IConnectionMultiplexer connectionMultiplexer =
                ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("evently-redis")!);
            builder.Services.TryAddSingleton(connectionMultiplexer);

            builder.Services.AddStackExchangeRedisCache(options =>
                options.ConnectionMultiplexerFactory = () => Task.FromResult(connectionMultiplexer));
        }
        catch
        {
            builder.Services.AddDistributedMemoryCache();
        }

        builder.Services.TryAddSingleton<ICacheService, CacheService>();
        builder.Services.TryAddSingleton<IEventBus, EventBus.EventBus>();
        builder.Services.AddMassTransit(configure =>
        {
            foreach (Action<IRegistrationConfigurator> configureConsumer in moduleConfigureConsumers)
            {
                configureConsumer(configure);
            }

            configure.SetKebabCaseEndpointNameFormatter();

            configure.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
        });
    }
}
