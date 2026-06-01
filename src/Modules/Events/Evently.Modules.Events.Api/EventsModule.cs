using Evently.Modules.Events.Api.Database;
using Evently.Modules.Events.Api.Events;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Hosting;

namespace Evently.Modules.Events.Api;

public static class EventsModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        CreateEvent.MapEndpoint(app);
        GetEvent.MapEndpoint(app);
    }

    public static void AddEventsModule(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<EventsDbContext>("evently", configureDbContextOptions: optionsBuilder =>
        {
            optionsBuilder.UseNpgsql(npgsqlOptions =>
                npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Events));
            optionsBuilder.UseSnakeCaseNamingConvention();
        });
    }
}
