using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Modules.Ticketing.Application.Abstractions.Data;
using Evently.Modules.Ticketing.Domain.Events;
using Evently.Modules.Ticketing.Domain.Tickets;

namespace Evently.Modules.Ticketing.Application.Tickets.ArchiveTicketsForEvent;

internal sealed class ArchiveTicketsForEventCommandHandler(
    IEventRepository eventRepository,
    ITicketRepository ticketRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ArchiveTicketsForEventCommand>
{
    public async Task<Result> HandleAsync(ArchiveTicketsForEventCommand request, CancellationToken cancellationToken)
    {
        // Tüm iş mantığını ve SaveChanges'ı stratejiye sarmalayıp gönderiyoruz
        return await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            Event? @event = await eventRepository.GetAsync(request.EventId, cancellationToken);

            if (@event is null) return Result.Failure(EventErrors.NotFound(request.EventId));

            IEnumerable<Ticket> tickets = await ticketRepository.GetForEventAsync(@event, cancellationToken);

            foreach (Ticket ticket in tickets) ticket.Archive();

            @event.TicketsArchived();

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }, cancellationToken);
    }
}
