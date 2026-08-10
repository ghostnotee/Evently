using Evently.Common.Infrastructure.Interceptors;
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
using Evently.Modules.Attendance.Infrastructure.Tickets;
using Evently.Modules.Attendance.Presentation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Evently.Modules.Attendance.Infrastructure;

public static class AttendanceModule
{
    extension(IHostApplicationBuilder builder)
    {
        public void AddEventsModule()
        {
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
                    .AddInterceptors(provider.GetRequiredService<PublishDomainEventsInterceptor>());
            });

            builder.EnrichNpgsqlDbContext<AttendanceDbContext>();
            builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AttendanceDbContext>());
            builder.Services.AddScoped<IAttendeeRepository, AttendeeRepository>();
            builder.Services.AddScoped<IEventRepository, EventRepository>();
            builder.Services.AddScoped<ITicketRepository, TicketRepository>();
            builder.Services.AddScoped<IAttendanceContext, AttendanceContext>();
        }
    }
}
