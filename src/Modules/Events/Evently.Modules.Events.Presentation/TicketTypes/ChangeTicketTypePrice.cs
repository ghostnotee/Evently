using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Common.Presentation.Endpoints;
using Evently.Common.Presentation.Results;
using Evently.Modules.Events.Application.TicketTypes.UpdateTicketTypePrice;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Evently.Modules.Events.Presentation.TicketTypes;

internal sealed class ChangeTicketTypePrice : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("ticket-types/{id:guid}/price", async (
                Guid id,
                Request request,
                ICommandHandler<UpdateTicketTypePriceCommand> handler,
                CancellationToken cancellationToken) =>
            {
                Result result = await handler.HandleAsync(new UpdateTicketTypePriceCommand(id, request.Price), cancellationToken);

                return result.Match(Results.NoContent, ApiResults.Problem);
            })
            .RequireAuthorization(Permissions.ModifyTicketTypes)
            .WithTags(Tags.TicketTypes);
    }

    internal sealed class Request
    {
        public decimal Price { get; init; }
    }
}
