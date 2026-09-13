using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Modules.Ticketing.Application.Abstractions.Data;
using Evently.Modules.Ticketing.Application.Abstractions.Payments;
using Evently.Modules.Ticketing.Application.Carts;
using Evently.Modules.Ticketing.Domain.Customers;
using Evently.Modules.Ticketing.Domain.Events;
using Evently.Modules.Ticketing.Domain.Orders;
using Evently.Modules.Ticketing.Domain.Payments;

namespace Evently.Modules.Ticketing.Application.Orders.CreateOrder;

internal sealed class CreateOrderCommandHandler(
    ICustomerRepository customerRepository,
    IOrderRepository orderRepository,
    ITicketTypeRepository ticketTypeRepository,
    IPaymentRepository paymentRepository,
    IPaymentService paymentService,
    CartService cartService,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateOrderCommand>
{
    public async Task<Result> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        Customer? customer = null;

        Result result = await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            customer = await customerRepository.GetAsync(request.CustomerId, cancellationToken);

            if (customer is null)
            {
                return Result.Failure(CustomerErrors.NotFound(request.CustomerId));
            }

            var order = Order.Create(customer);

            Cart cart = await cartService.GetAsync(customer.Id, cancellationToken);

            if (!cart.Items.Any())
            {
                return Result.Failure(CartErrors.Empty);
            }

            foreach (CartItem cartItem in cart.Items)
            {
                // This acquires a pessimistic lock or throws an exception if already locked.
                TicketType? ticketType = await ticketTypeRepository.GetWithLockAsync(
                    cartItem.TicketTypeId,
                    cancellationToken);

                if (ticketType is null)
                {
                    return Result.Failure(TicketTypeErrors.NotFound(cartItem.TicketTypeId));
                }

                Result ticketTypeResult = ticketType.UpdateQuantity(cartItem.Quantity);

                if (ticketTypeResult.IsFailure)
                {
                    return Result.Failure(ticketTypeResult.Error);
                }

                order.AddItem(ticketType, cartItem.Quantity, cartItem.Price, ticketType.Currency);
            }

            orderRepository.Insert(order);

            // We're faking a payment gateway request here...
            PaymentResponse paymentResponse = await paymentService.ChargeAsync(order.TotalPrice, order.Currency);

            var payment = Payment.Create(
                order,
                paymentResponse.TransactionId,
                paymentResponse.Amount,
                paymentResponse.Currency);

            paymentRepository.Insert(payment);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }, cancellationToken);

        if (result.IsSuccess && customer is not null)
        {
            await cartService.ClearAsync(customer.Id, cancellationToken);
        }

        return result;
    }
}
