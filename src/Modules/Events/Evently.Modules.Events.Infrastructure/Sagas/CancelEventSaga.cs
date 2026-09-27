using Evently.Modules.Events.IntegrationEvents;
using Evently.Modules.Ticketing.IntegrationEvents;
using Wolverine;
using Wolverine.Persistence.Sagas;

namespace Evently.Modules.Events.Infrastructure.Sagas;

public sealed class CancelEventSaga : Saga
{
    public Guid Id { get; set; }

    public bool PaymentsRefunded { get; set; }
    public bool TicketsArchived { get; set; }

    public static (CancelEventSaga, EventCancellationStartedIntegrationEvent) Start(
        EventCanceledIntegrationEvent message)
    {
        var saga = new CancelEventSaga
        {
            Id = message.EventId
        };

        var started = new EventCancellationStartedIntegrationEvent(
            message.Id,
            message.OccurredOnUtc,
            message.EventId);

        return (saga, started);
    }

    public EventCancellationCompletedIntegrationEvent? Handle(
        [SagaIdentityFrom(nameof(EventPaymentsRefundedIntegrationEvent.EventId))]
        EventPaymentsRefundedIntegrationEvent message)
    {
        PaymentsRefunded = true;
        return TryComplete();
    }

    public EventCancellationCompletedIntegrationEvent? Handle(
        [SagaIdentityFrom(nameof(EventTicketsArchivedIntegrationEvent.EventId))]
        EventTicketsArchivedIntegrationEvent message)
    {
        TicketsArchived = true;
        return TryComplete();
    }

    public static void NotFound(
        [SagaIdentityFrom(nameof(EventPaymentsRefundedIntegrationEvent.EventId))]
        EventPaymentsRefundedIntegrationEvent message)
    {
        // Handle not found
    }

    public static void NotFound(
        [SagaIdentityFrom(nameof(EventTicketsArchivedIntegrationEvent.EventId))]
        EventTicketsArchivedIntegrationEvent message)
    {
        // Handle not found
    }

    private EventCancellationCompletedIntegrationEvent? TryComplete()
    {
        if (!PaymentsRefunded || !TicketsArchived)
        {
            return null;
        }

        MarkCompleted();

        return new EventCancellationCompletedIntegrationEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            Id);
    }
}
