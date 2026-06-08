using Evently.Common.Presentation.Endpoints;
using Evently.Modules.Events.Application.Abstractions.Data;
using Evently.Modules.Events.Domain.Categories;
using Evently.Modules.Events.Domain.Events;
using Evently.Modules.Events.Domain.TicketTypes;
using Evently.Modules.Events.Infrastructure.Categories;
using Evently.Modules.Events.Infrastructure.Database;
using Evently.Modules.Events.Infrastructure.Events;
using Evently.Modules.Events.Infrastructure.TicketTypes;
using Evently.Modules.Events.Presentation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Evently.Modules.Events.Infrastructure;

public static class EventsModule
{
    extension(IHostApplicationBuilder builder)
    {
        public IServiceCollection AddEventsModule()
        {
            builder.Services.AddEndpoints(AssemblyReference.Assembly);
            builder.AddInfrastructure();

            return builder.Services;
        }

        private void AddInfrastructure()
        {
            builder.AddNpgsqlDbContext<EventsDbContext>("evently-db", null,
                optionsBuilder =>
                {
                    optionsBuilder.UseNpgsql(npgsqlOptions =>
                            npgsqlOptions.MigrationsHistoryTable(
                                HistoryRepository.DefaultTableName,
                                Schemas.Events))
                        .UseSnakeCaseNamingConvention()
                        .AddInterceptors();
                });

            builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<EventsDbContext>());

            builder.Services.AddScoped<IEventRepository, EventRepository>();
            builder.Services.AddScoped<ITicketTypeRepository, TicketTypeRepository>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
        }
    }
}
