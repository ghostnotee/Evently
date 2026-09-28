using Evently.Common.Application.Exceptions;
using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Modules.Ticketing.Application.Orders.GetOrder;
using Evently.Modules.Ticketing.Domain.Orders;

namespace Evently.Modules.Ticketing.Application.Orders.CreateOrder;

internal sealed class SendOrderConfirmationDomainEventHandler(
    IQueryHandler<GetOrderQuery, OrderResponse> handler) : DomainEventHandler<OrderCreatedDomainEvent>
{
    public override async Task HandleAsync(
        OrderCreatedDomainEvent notification,
        CancellationToken cancellationToken = default)
    {
        Result<OrderResponse> result = await handler.HandleAsync(new GetOrderQuery(notification.OrderId), cancellationToken);

        if (result.IsFailure) throw new EventlyException(nameof(GetOrderQuery), result.Error);

        // Send order confirmation notification.
    }
}
