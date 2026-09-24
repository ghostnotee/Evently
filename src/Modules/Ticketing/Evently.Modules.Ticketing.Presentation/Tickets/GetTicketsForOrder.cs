using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Common.Presentation.Endpoints;
using Evently.Common.Presentation.Results;
using Evently.Modules.Ticketing.Application.Tickets.GetTicket;
using Evently.Modules.Ticketing.Application.Tickets.GetTicketForOrder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Evently.Modules.Ticketing.Presentation.Tickets;

internal sealed class GetTicketsForOrder : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("tickets/order/{orderId:guid}", async (
                Guid orderId,
                IQueryHandler<GetTicketsForOrderQuery, IReadOnlyCollection<TicketResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                Result<IReadOnlyCollection<TicketResponse>> result = await handler.HandleAsync(
                    new GetTicketsForOrderQuery(orderId), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .RequireAuthorization(Permissions.GetTickets)
            .WithTags(Tags.Tickets);
    }
}
