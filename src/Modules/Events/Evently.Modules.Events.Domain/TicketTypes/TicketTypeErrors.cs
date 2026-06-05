using Evently.Common.Domain;

namespace Evently.Modules.Events.Domain.TicketTypes;

public static class TicketTypeErrors
{
    public static Error NotFound(Guid ticketTypeId)
    {
        return Error.NotFound("TicketTypes.NotFound",
            $"The ticket type with the identifier {ticketTypeId} was not found");
    }
}
