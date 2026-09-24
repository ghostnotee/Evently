using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Common.Presentation.Endpoints;
using Evently.Common.Presentation.Results;
using Evently.Modules.Ticketing.Application.Abstractions.Authentication;
using Evently.Modules.Ticketing.Application.Orders.GetOrders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Evently.Modules.Ticketing.Presentation.Orders;

internal sealed class GetOrders : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("orders", async (
                ICustomerContext customerContext,
                IQueryHandler<GetOrdersQuery, IReadOnlyCollection<OrderResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                Result<IReadOnlyCollection<OrderResponse>> result = await handler.HandleAsync(
                    new GetOrdersQuery(customerContext.CustomerId), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .RequireAuthorization(Permissions.GetOrders)
            .WithTags(Tags.Orders);
    }
}
