using Evently.Common.Application.Data;
using Evently.Modules.Events.IntegrationEvents;
using Evently.Modules.Users.IntegrationEvents;

namespace Evently.Modules.Ticketing.Infrastructure.Inbox;

public sealed class UserRegisteredIntegrationEventConsumer(IDbConnectionFactory dbConnectionFactory)
    : IntegrationEventConsumer<UserRegisteredIntegrationEvent>(dbConnectionFactory);

public sealed class UserProfileUpdatedIntegrationEventConsumer(IDbConnectionFactory dbConnectionFactory)
    : IntegrationEventConsumer<UserProfileUpdatedIntegrationEvent>(dbConnectionFactory);

public sealed class EventPublishedIntegrationEventConsumer(IDbConnectionFactory dbConnectionFactory)
    : IntegrationEventConsumer<EventPublishedIntegrationEvent>(dbConnectionFactory);

public sealed class TicketTypePriceChangedIntegrationEventConsumer(IDbConnectionFactory dbConnectionFactory)
    : IntegrationEventConsumer<TicketTypePriceChangedIntegrationEvent>(dbConnectionFactory);

public sealed class EventCancellationStartedIntegrationEventConsumer(IDbConnectionFactory dbConnectionFactory)
    : IntegrationEventConsumer<EventCancellationStartedIntegrationEvent>(dbConnectionFactory);
