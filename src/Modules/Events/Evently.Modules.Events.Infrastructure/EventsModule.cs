using Evently.Modules.Events.Api.Database;
using Evently.Modules.Events.Application;
using Evently.Modules.Events.Application.Abstractions.Data;
using Evently.Modules.Events.Domain.Events;
using Evently.Modules.Events.Infrastructure.Data;
using Evently.Modules.Events.Infrastructure.Database;
using Evently.Modules.Events.Infrastructure.Events;
using Evently.Modules.Events.Presentation.Events;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Evently.Modules.Events.Infrastructure;

public static class EventsModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        EventEndpoints.MapEndpoints(app);
    }

    extension(IHostApplicationBuilder builder)
    {
        public void AddEventsModule()
        {
            builder.Services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(AssemblyReference.Assembly);
            });
            builder.AddInfrastructure();
        }

        private void AddInfrastructure()
        {
            builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
            builder.AddNpgsqlDbContext<EventsDbContext>("evently", configureDbContextOptions: optionsBuilder =>
            {
                optionsBuilder.UseNpgsql(npgsqlOptions =>
                    npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Events));
                optionsBuilder.UseSnakeCaseNamingConvention();
            });
            builder.Services.AddScoped<IEventRepository, EventRepository>();
            builder.Services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<EventsDbContext>());
        }
    }
}
