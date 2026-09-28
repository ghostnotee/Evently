using Evently.Common.Application.Exceptions;
using Evently.Common.Application.Messaging;
using Evently.Modules.Ticketing.Application.Abstractions.Data;
using Evently.Modules.Ticketing.Domain.Orders;
using Evently.Modules.Ticketing.Domain.Payments;

namespace Evently.Modules.Ticketing.Application.Payments.RefundPayment;

internal sealed class PaymentRefundedOrderDomainEventHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork)
    : DomainEventHandler<PaymentRefundedDomainEvent>
{
    public override async Task HandleAsync(PaymentRefundedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        Order? order = await orderRepository.GetAsync(domainEvent.OrderId, cancellationToken) ?? 
                       throw new EventlyException(nameof(IOrderRepository), OrderErrors.NotFound(domainEvent.OrderId));

        order.Refund();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
