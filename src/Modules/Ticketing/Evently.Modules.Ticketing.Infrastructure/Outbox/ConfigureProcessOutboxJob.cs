using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Evently.Modules.Ticketing.Infrastructure.Outbox;

internal static class ConfigureProcessOutboxJob
{
    internal static void Configure(IServiceCollection services, int intervalInSeconds)
    {
        services.ConfigureAllQuartzSchedulers(options =>
        {
            string jobName = typeof(ProcessOutboxJob).FullName!;

            options
                .AddJob<ProcessOutboxJob>(configure => configure.WithIdentity(jobName))
                .AddTrigger(configure =>
                    configure
                        .ForJob(jobName)
                        .WithSimpleSchedule(schedule =>
                            schedule.WithInterval(TimeSpan.FromSeconds(intervalInSeconds)).RepeatForever()));
        });
    }
}
