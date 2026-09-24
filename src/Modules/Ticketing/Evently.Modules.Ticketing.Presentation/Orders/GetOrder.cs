using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Common.Presentation.Endpoints;
using Evently.Common.Presentation.Results;
using Evently.Modules.Ticketing.Application.Orders.GetOrder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Evently.Modules.Ticketing.Presentation.Orders;

internal sealed class GetOrder : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("orders/{id:guid}", async (
                Guid id,
                IQueryHandler<GetOrderQuery, OrderResponse> handler,
                CancellationToken cancellationToken) =>
            {
                Result<OrderResponse> result = await handler.HandleAsync(new GetOrderQuery(id), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .WithTags(Tags.Orders);
    }
}
