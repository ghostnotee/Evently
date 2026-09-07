using System.Data.Common;
using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Modules.Ticketing.Application.Abstractions.Data;
using Evently.Modules.Ticketing.Domain.Events;
using Evently.Modules.Ticketing.Domain.Payments;

namespace Evently.Modules.Ticketing.Application.Payments.RefundPaymentsForEvent;

internal sealed class RefundPaymentsForEventCommandHandler(
    IEventRepository eventRepository,
    IPaymentRepository paymentRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RefundPaymentsForEventCommand>
{
    public async Task<Result> Handle(RefundPaymentsForEventCommand request, CancellationToken cancellationToken)
    {
        return await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            Event? @event = await eventRepository.GetAsync(request.EventId, cancellationToken);

            if (@event is null)
            {
                return Result.Failure(EventErrors.NotFound(request.EventId));
            }

            IEnumerable<Payment> payments = await paymentRepository.GetForEventAsync(@event, cancellationToken);

            foreach (Payment payment in payments)
            {
                payment.Refund(payment.Amount - (payment.AmountRefunded ?? decimal.Zero));
            }

            @event.PaymentsRefunded();

            return Result.Success();
        }, cancellationToken);
    }
}
