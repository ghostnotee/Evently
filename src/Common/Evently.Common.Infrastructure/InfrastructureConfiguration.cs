using Evently.Common.Application.Caching;
using Evently.Common.Application.Clock;
using Evently.Common.Application.Data;
using Evently.Common.Infrastructure.Caching;
using Evently.Common.Infrastructure.Clock;
using Evently.Common.Infrastructure.Data;
using Evently.Common.Infrastructure.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Evently.Common.Infrastructure;

public static class InfrastructureConfiguration
{
    public static void AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDataSource("evently-db");

        builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

        builder.Services.TryAddSingleton<PublishDomainEventsInterceptor>();

        builder.Services.TryAddSingleton<IDateTimeProvider, DateTimeProvider>();

        builder.AddRedisDistributedCache("evently-redis");

        builder.Services.TryAddSingleton<ICacheService, CacheService>();
    }
}
