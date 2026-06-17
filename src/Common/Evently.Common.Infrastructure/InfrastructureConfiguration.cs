using Evently.Common.Application.Caching;
using Evently.Common.Application.Clock;
using Evently.Common.Application.Data;
using Evently.Common.Infrastructure.Caching;
using Evently.Common.Infrastructure.Clock;
using Evently.Common.Infrastructure.Data;
using Evently.Common.Infrastructure.Interceptors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace Evently.Common.Infrastructure;

public static class InfrastructureConfiguration
{
    public static void AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDataSource("evently-db");

        builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

        builder.Services.TryAddSingleton<PublishDomainEventsInterceptor>();

        builder.Services.TryAddSingleton<IDateTimeProvider, DateTimeProvider>();

        try
        {
            IConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("evently-redis")!);
            builder.Services.TryAddSingleton(connectionMultiplexer);

            builder.Services.AddStackExchangeRedisCache(options =>
                options.ConnectionMultiplexerFactory = () => Task.FromResult(connectionMultiplexer));
        }
        catch
        {
            builder.Services.AddDistributedMemoryCache();
        }

        builder.Services.TryAddSingleton<ICacheService, CacheService>();
    }
}
