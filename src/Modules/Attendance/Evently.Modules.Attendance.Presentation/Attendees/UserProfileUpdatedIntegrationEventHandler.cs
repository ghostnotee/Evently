using Evently.Common.Application.EventBus;
using Evently.Common.Application.Exceptions;
using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Modules.Attendance.Application.Attendees.UpdateAttendee;
using Evently.Modules.Users.IntegrationEvents;

namespace Evently.Modules.Attendance.Presentation.Attendees;

internal sealed class UserProfileUpdatedIntegrationEventHandler(
    ICommandHandler<UpdateAttendeeCommand> handler) : IntegrationEventHandler<UserProfileUpdatedIntegrationEvent>
{
    public override async Task Handle(
        UserProfileUpdatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        Result result = await handler.HandleAsync(
            new UpdateAttendeeCommand(
                integrationEvent.UserId,
                integrationEvent.FirstName,
                integrationEvent.LastName),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new EventlyException(nameof(UpdateAttendeeCommand), result.Error);
        }
    }
}
