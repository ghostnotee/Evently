using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Common.Presentation.Endpoints;
using Evently.Common.Presentation.Results;
using Evently.Modules.Ticketing.Application.Abstractions.Authentication;
using Evently.Modules.Ticketing.Application.Carts.ClearCart;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Evently.Modules.Ticketing.Presentation.Carts;

internal sealed class ClearCart : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("carts", async (
                ICustomerContext customerContext,
                ICommandHandler<ClearCartCommand> handler,
                CancellationToken cancellationToken) =>
            {
                Result result = await handler.HandleAsync(new ClearCartCommand(customerContext.CustomerId), cancellationToken);

                return result.Match(() => Results.Ok(), ApiResults.Problem);
            })
            .RequireAuthorization(Permissions.RemoveFromCart)
            .WithTags(Tags.Carts);
    }
}
