using Evently.Common.Domain;

namespace Evently.Common.Application.Messaging;

public interface IDomainEventHandler<in TDomainEvent> : IDomainEventHandler where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}

public interface IDomainEventHandler
{
    Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
