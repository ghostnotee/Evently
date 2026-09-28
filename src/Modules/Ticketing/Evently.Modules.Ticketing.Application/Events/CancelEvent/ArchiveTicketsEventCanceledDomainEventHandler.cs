using Evently.Common.Application.Exceptions;
using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Modules.Ticketing.Application.Tickets.ArchiveTicketsForEvent;
using Evently.Modules.Ticketing.Domain.Events;

namespace Evently.Modules.Ticketing.Application.Events.CancelEvent;

internal sealed class ArchiveTicketsEventCanceledDomainEventHandler(
    ICommandHandler<ArchiveTicketsForEventCommand> handler) : DomainEventHandler<EventCanceledDomainEvent>
{
    public override async Task HandleAsync(
        EventCanceledDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        Result result = await handler.HandleAsync(new ArchiveTicketsForEventCommand(domainEvent.EventId), cancellationToken);

        if (result.IsFailure) throw new EventlyException(nameof(ArchiveTicketsForEventCommand), result.Error);
    }
}
