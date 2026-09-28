using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Evently.Modules.Attendance.Infrastructure.Inbox;

internal static class ConfigureProcessInboxJob
{
    internal static void Configure(IServiceCollection services, int intervalInSeconds)
    {
        services.ConfigureAllQuartzSchedulers(options =>
        {
            string jobName = typeof(ProcessInboxJob).FullName!;

            options
                .AddJob<ProcessInboxJob>(configure => configure.WithIdentity(jobName))
                .AddTrigger(configure =>
                    configure
                        .ForJob(jobName)
                        .WithSimpleSchedule(schedule =>
                            schedule.WithInterval(TimeSpan.FromSeconds(intervalInSeconds)).RepeatForever()));
        });
    }
}
