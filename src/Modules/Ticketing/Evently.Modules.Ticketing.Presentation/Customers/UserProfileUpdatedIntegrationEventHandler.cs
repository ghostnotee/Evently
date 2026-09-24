using Evently.Common.Application.EventBus;
using Evently.Common.Application.Exceptions;
using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Modules.Ticketing.Application.Customers.UpdateCustomer;
using Evently.Modules.Users.IntegrationEvents;

namespace Evently.Modules.Ticketing.Presentation.Customers;

internal sealed class UserProfileUpdatedIntegrationEventHandler(
    ICommandHandler<UpdateCustomerCommand> handler) : IntegrationEventHandler<UserProfileUpdatedIntegrationEvent>
{
    public override async Task Handle(
        UserProfileUpdatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        Result result = await handler.HandleAsync(
            new UpdateCustomerCommand(
                integrationEvent.UserId,
                integrationEvent.FirstName,
                integrationEvent.LastName),
            cancellationToken);

        if (result.IsFailure) throw new EventlyException(nameof(UpdateCustomerCommand), result.Error);
    }
}
