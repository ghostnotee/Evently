using Evently.Common.Application.Clock;
using Evently.Common.Application.Data;
using Evently.Common.Infrastructure.Clock;
using Evently.Common.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Evently.Common.Infrastructure;

public static class InfrastructureConfiguration
{
    public static void AddInfrastructure(
        this IHostApplicationBuilder builder)
    {
        // Aspire üzerinden — tracing aktif olur
        builder.AddNpgsqlDataSource("evently");

        builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
        builder.Services.TryAddSingleton<IDateTimeProvider, DateTimeProvider>();
    }
}
