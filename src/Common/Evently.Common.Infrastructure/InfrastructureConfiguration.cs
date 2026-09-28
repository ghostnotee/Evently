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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Quartz;
using StackExchange.Redis;

namespace Evently.Common.Infrastructure;

public static class InfrastructureConfiguration
{
    public static void AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.Services.AddAuthenticationInternal();

        builder.Services.AddAuthorizationInternal();

        builder.AddNpgsqlDataSource("evently-db");

        builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
        SqlMapper.AddTypeHandler(new GenericArrayHandler<string>());

        builder.Services.AddQuartz(configurator =>
        {
            var scheduler = Guid.NewGuid();
            configurator.ConfigureScheduler(options =>
            {
                options.InstanceId = $"default-id-{scheduler}";
                options.InstanceName = $"default-name-{scheduler}";
            });
        });
        builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        builder.Services.TryAddSingleton<InsertOutboxMessagesInterceptor>();

        builder.Services.TryAddSingleton<IDateTimeProvider, DateTimeProvider>();

        try
        {
            IConnectionMultiplexer connectionMultiplexer =
                ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("evently-cache")!);
            builder.Services.TryAddSingleton(connectionMultiplexer);

            builder.Services.AddStackExchangeRedisCache(options =>
                options.ConnectionMultiplexerFactory = () => Task.FromResult(connectionMultiplexer));
        }
        catch
        {
            builder.Services.AddDistributedMemoryCache();
        }

        builder.Services.TryAddSingleton<ICacheService, CacheService>();

        builder.Services.TryAddScoped<IEventBus, EventBus.EventBus>();

        builder.AddRabbitMQClient("evently-queue");

        builder.AddMongoDBClient("evently-mongo");
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }
}
