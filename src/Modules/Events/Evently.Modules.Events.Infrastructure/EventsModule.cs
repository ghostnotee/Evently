using Evently.Common.Application.EventBus;
using Evently.Common.Application.Messaging;
using Evently.Common.Infrastructure.Outbox;
using Evently.Common.Presentation.Endpoints;
using Evently.Modules.Events.Application.Abstractions.Data;
using Evently.Modules.Events.Domain.Categories;
using Evently.Modules.Events.Domain.Events;
using Evently.Modules.Events.Domain.TicketTypes;
using Evently.Modules.Events.Infrastructure.Categories;
using Evently.Modules.Events.Infrastructure.Database;
using Evently.Modules.Events.Infrastructure.Events;
using Evently.Modules.Events.Infrastructure.Inbox;
using Evently.Modules.Events.Infrastructure.Outbox;
using Evently.Modules.Events.Infrastructure.TicketTypes;
using Evently.Modules.Events.Presentation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Evently.Modules.Events.Infrastructure;

public static class EventsModule
{
    extension(IHostApplicationBuilder builder)
    {
        public void AddEventsModule()
        {
            builder.Services.AddDomainEventHandlers();
            builder.Services.AddIntegrationEventHandlers();
            builder.AddInfrastructure();
            builder.Services.AddEndpoints(AssemblyReference.Assembly);
        }

        private void AddInfrastructure()
        {
            string connectionString = builder.Configuration.GetConnectionString("evently-db")
                                      ?? throw new InvalidOperationException(
                                          "Connection string 'evently-db' was not found.");
            builder.Services.AddDbContext<EventsDbContext>((provider, optionsBuilder) =>
            {
                optionsBuilder.UseNpgsql(connectionString,
                        contextOptionsBuilder => contextOptionsBuilder
                            .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Events))
                    .UseSnakeCaseNamingConvention()
                    .AddInterceptors(provider.GetRequiredService<InsertOutboxMessagesInterceptor>());
            });

            builder.EnrichNpgsqlDbContext<EventsDbContext>();

            builder.Services.AddScoped<IEventRepository, EventRepository>();
            builder.Services.AddScoped<ITicketTypeRepository, TicketTypeRepository>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

            builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<EventsDbContext>());
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
