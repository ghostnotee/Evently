using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Common.Presentation.Endpoints;
using Evently.Common.Presentation.Results;
using Evently.Modules.Events.Application.TicketTypes.GetTicketType;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Evently.Modules.Events.Presentation.TicketTypes;

internal sealed class GetTicketType : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("ticket-types/{id:guid}", async (
                Guid id,
                IQueryHandler<GetTicketTypeQuery, TicketTypeResponse> handler,
                CancellationToken cancellationToken) =>
            {
                Result<TicketTypeResponse> result = await handler.HandleAsync(new GetTicketTypeQuery(id), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .RequireAuthorization(Permissions.GetTicketTypes)
            .WithTags(Tags.TicketTypes);
    }
}
