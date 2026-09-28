using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Modules.Ticketing.Application.Abstractions.Data;
using Evently.Modules.Ticketing.Domain.Events;
using Evently.Modules.Ticketing.Domain.Payments;

namespace Evently.Modules.Ticketing.Application.Payments.RefundPaymentsForEvent;

internal sealed class RefundPaymentsForEventCommandHandler(
    IEventRepository eventRepository,
    IPaymentRepository paymentRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RefundPaymentsForEventCommand>
{
    public async Task<Result> HandleAsync(RefundPaymentsForEventCommand request, CancellationToken cancellationToken)
    {
        return await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            Event? @event = await eventRepository.GetAsync(request.EventId, cancellationToken);

            if (@event is null) return Result.Failure(EventErrors.NotFound(request.EventId));

            IEnumerable<Payment> payments = await paymentRepository.GetForEventAsync(@event, cancellationToken);

            foreach (Payment payment in payments)
            {
                // Kalan tutarı hesapla
                decimal remainingAmount = payment.Amount - (payment.AmountRefunded ?? decimal.Zero);

                // Eğer zaten tamamen iade edilmişse bu ödemeyi atla (veya hata dönün)
                if (remainingAmount <= 0) continue;

                // Refund sonucunu yakala ve kontrol et
                Result refundResult = payment.Refund(remainingAmount);

                if (refundResult.IsFailure)
                    // Herhangi bir ödemede hata çıkarsa tüm transaction iptal olsun diye hata dönüyoruz
                    return refundResult;
            }

            @event.PaymentsRefunded();

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }, cancellationToken);
    }
}
