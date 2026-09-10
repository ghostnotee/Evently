using Evently.Common.Application.EventBus;
using Evently.Common.Application.Messaging;
using Evently.Common.Infrastructure.Outbox;
using Evently.Common.Presentation.Endpoints;
using Evently.Modules.Attendance.Application.Abstractions.Authentication;
using Evently.Modules.Attendance.Application.Abstractions.Data;
using Evently.Modules.Attendance.Domain.Attendees;
using Evently.Modules.Attendance.Domain.Events;
using Evently.Modules.Attendance.Domain.Tickets;
using Evently.Modules.Attendance.Infrastructure.Attendees;
using Evently.Modules.Attendance.Infrastructure.Authentication;
using Evently.Modules.Attendance.Infrastructure.Database;
using Evently.Modules.Attendance.Infrastructure.Events;
using Evently.Modules.Attendance.Infrastructure.Inbox;
using Evently.Modules.Attendance.Infrastructure.Outbox;
using Evently.Modules.Attendance.Infrastructure.Tickets;
using Evently.Modules.Attendance.Presentation;
using Evently.Modules.Events.IntegrationEvents;
using Evently.Modules.Ticketing.IntegrationEvents;
using Evently.Modules.Users.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Evently.Modules.Attendance.Infrastructure;

public static class AttendanceModule
{
    public static void ConfigureConsumers(IRegistrationConfigurator registrationConfigurator)
    {
        registrationConfigurator.AddConsumer<IntegrationEventConsumer<UserRegisteredIntegrationEvent>>();
        registrationConfigurator.AddConsumer<IntegrationEventConsumer<UserProfileUpdatedIntegrationEvent>>();
        registrationConfigurator.AddConsumer<IntegrationEventConsumer<EventPublishedIntegrationEvent>>();
        registrationConfigurator.AddConsumer<IntegrationEventConsumer<TicketIssuedIntegrationEvent>>();
        registrationConfigurator.AddConsumer<IntegrationEventConsumer<EventCancellationStartedIntegrationEvent>>();
    }

    extension(IHostApplicationBuilder builder)
    {
        public void AddAttendanceModule()
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
            builder.Services.AddDbContext<AttendanceDbContext>((provider, optionsBuilder) =>
            {
                optionsBuilder.UseNpgsql(connectionString,
                        contextOptionsBuilder => contextOptionsBuilder
                            .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Attendance))
                    .UseSnakeCaseNamingConvention()
                    .AddInterceptors(provider.GetRequiredService<InsertOutboxMessagesInterceptor>());
            });

            builder.EnrichNpgsqlDbContext<AttendanceDbContext>();
            builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AttendanceDbContext>());
            builder.Services.AddScoped<IAttendeeRepository, AttendeeRepository>();
            builder.Services.AddScoped<IEventRepository, EventRepository>();
            builder.Services.AddScoped<ITicketRepository, TicketRepository>();
            builder.Services.AddScoped<IAttendanceContext, AttendanceContext>();
            
            builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Attendance:Outbox"));
            builder.Services.ConfigureOptions<ConfigureProcessOutboxJob>();

            builder.Services.Configure<InboxOptions>(builder.Configuration.GetSection("Attendance:Inbox"));
            builder.Services.ConfigureOptions<ConfigureProcessInboxJob>();
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
