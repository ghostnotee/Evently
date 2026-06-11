using Evently.Common.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Evently.Common.Infrastructure.Interceptors;

public sealed class PublishDomainEventsInterceptor(IServiceScopeFactory serviceScopeFactory) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await PublishDomainEventsAsync(eventData.Context, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private async Task PublishDomainEventsAsync(DbContext context, CancellationToken cancellationToken)
    {
        List<(Entity Entity, IReadOnlyCollection<IDomainEvent> Events)> entitiesWithEvents = context
            .ChangeTracker
            .Entries<Entity>()
            .Select(entry => entry.Entity)
            .Select(entity => (Entity: entity, Events: entity.DomainEvents))
            .Where(x => x.Events.Count > 0)
            .ToList();

        if (entitiesWithEvents.Count == 0)
        {
            return;
        }

        using IServiceScope scope = serviceScopeFactory.CreateScope();

        IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        foreach (var (_, events) in entitiesWithEvents)
        {
            foreach (IDomainEvent domainEvent in events)
            {
                await publisher.Publish(domainEvent, cancellationToken);
            }
        }

        // Only clear after successful publish so a failed publish can be retried.
        foreach (var (entity, _) in entitiesWithEvents)
        {
            entity.ClearDomainEvents();
        }
    }
}
