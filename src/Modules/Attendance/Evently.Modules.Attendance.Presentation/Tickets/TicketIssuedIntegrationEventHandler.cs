using Evently.Common.Application.EventBus;
using Evently.Common.Application.Exceptions;
using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Modules.Attendance.Application.Tickets.CreateTicket;
using Evently.Modules.Ticketing.IntegrationEvents;

namespace Evently.Modules.Attendance.Presentation.Tickets;

internal sealed class TicketIssuedIntegrationEventHandler(
    ICommandHandler<CreateTicketCommand> handler) : IntegrationEventHandler<TicketIssuedIntegrationEvent>
{
    public override async Task Handle(
        TicketIssuedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        Result result = await handler.HandleAsync(
            new CreateTicketCommand(
                integrationEvent.TicketId,
                integrationEvent.CustomerId,
                integrationEvent.EventId,
                integrationEvent.Code),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new EventlyException(nameof(CreateTicketCommand), result.Error);
        }
    }
}
