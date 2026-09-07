using System.Data.Common;
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
        // 1. İş mantığını UnitOfWork'e delegate (temsilci) olarak veriyoruz.
        Result result = await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            Customer? customer = await customerRepository.GetAsync(request.CustomerId, cancellationToken);
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
                TicketType? ticketType = await ticketTypeRepository.GetWithLockAsync(
                    cartItem.TicketTypeId,
                    cancellationToken);

                if (ticketType is null)
                {
                    return Result.Failure(TicketTypeErrors.NotFound(cartItem.TicketTypeId));
                }

                Result updateResult = ticketType.UpdateQuantity(cartItem.Quantity);
                if (updateResult.IsFailure)
                {
                    return updateResult;
                }

                order.AddItem(ticketType, cartItem.Quantity, cartItem.Price, ticketType.Currency);
            }

            orderRepository.Insert(order);

            // Ödeme servisi çağrısı
            PaymentResponse paymentResponse = await paymentService.ChargeAsync(order.TotalPrice, order.Currency);

            var payment = Payment.Create(
                order,
                paymentResponse.TransactionId,
                paymentResponse.Amount,
                paymentResponse.Currency);

            paymentRepository.Insert(payment);

            // İşlem başarılı, Result.Success dönüyoruz. 
            // UnitOfWork bunu görecek, SaveChanges ve Commit yapacak.
            return Result.Success();

        }, cancellationToken);

        // 2. Eğer transaction başarısız olduysa (Result.Failure döndüyse), direkt hatayı dön.
        if (result.IsFailure)
        {
            return result;
        }

        // 3. Transaction başarıyla commit edildikten SONRA veritabanı dışı işlemleri yap.
        // (Örneğin Redis'teki sepeti temizlemek veya RabbitMQ'ya event fırlatmak)
        await cartService.ClearAsync(request.CustomerId, cancellationToken);

        return Result.Success();
    }
    
    // public async Task<Result> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    // {
    //     await using DbTransaction transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
    //
    //     Customer? customer = await customerRepository.GetAsync(request.CustomerId, cancellationToken);
    //
    //     if (customer is null)
    //     {
    //         return Result.Failure(CustomerErrors.NotFound(request.CustomerId));
    //     }
    //
    //     var order = Order.Create(customer);
    //
    //     Cart cart = await cartService.GetAsync(customer.Id, cancellationToken);
    //
    //     if (!cart.Items.Any())
    //     {
    //         return Result.Failure(CartErrors.Empty);
    //     }
    //
    //     foreach (CartItem cartItem in cart.Items)
    //     {
    //         // This acquires a pessimistic lock or throws an exception if already locked.
    //         TicketType? ticketType = await ticketTypeRepository.GetWithLockAsync(
    //             cartItem.TicketTypeId,
    //             cancellationToken);
    //
    //         if (ticketType is null)
    //         {
    //             return Result.Failure(TicketTypeErrors.NotFound(cartItem.TicketTypeId));
    //         }
    //
    //         Result result = ticketType.UpdateQuantity(cartItem.Quantity);
    //
    //         if (result.IsFailure)
    //         {
    //             return Result.Failure(result.Error);
    //         }
    //
    //         order.AddItem(ticketType, cartItem.Quantity, cartItem.Price, ticketType.Currency);
    //     }
    //
    //     orderRepository.Insert(order);
    //
    //     // We're faking a payment gateway request here...
    //     PaymentResponse paymentResponse = await paymentService.ChargeAsync(order.TotalPrice, order.Currency);
    //
    //     var payment = Payment.Create(
    //         order,
    //         paymentResponse.TransactionId,
    //         paymentResponse.Amount,
    //         paymentResponse.Currency);
    //
    //     paymentRepository.Insert(payment);
    //
    //     await unitOfWork.SaveChangesAsync(cancellationToken);
    //
    //     await transaction.CommitAsync(cancellationToken);
    //
    //     await cartService.ClearAsync(customer.Id, cancellationToken);
    //
    //     return Result.Success();
    // }
}
