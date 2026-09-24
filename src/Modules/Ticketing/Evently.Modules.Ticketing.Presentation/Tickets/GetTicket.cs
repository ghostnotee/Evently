using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Common.Presentation.Endpoints;
using Evently.Common.Presentation.Results;
using Evently.Modules.Ticketing.Application.Tickets.GetTicket;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Evently.Modules.Ticketing.Presentation.Tickets;

internal sealed class GetTicket : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("tickets/{id:guid}", async (
                Guid id,
                IQueryHandler<GetTicketQuery, TicketResponse> handler,
                CancellationToken cancellationToken) =>
            {
                Result<TicketResponse> result = await handler.HandleAsync(new GetTicketQuery(id), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .RequireAuthorization(Permissions.GetTickets)
            .WithTags(Tags.Tickets);
    }
}
