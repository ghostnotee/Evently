using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Common.Presentation.Endpoints;
using Evently.Common.Presentation.Results;
using Evently.Modules.Attendance.Application.Abstractions.Authentication;
using Evently.Modules.Attendance.Application.Attendees.CheckInAttendee;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Evently.Modules.Attendance.Presentation.Attendees;

internal sealed class CheckInAttendee : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("attendees/check-in", async (
                Request request,
                IAttendanceContext attendanceContext,
                ICommandHandler<CheckInAttendeeCommand> handler,
                CancellationToken cancellationToken) =>
            {
                Result result = await handler.HandleAsync(
                    new CheckInAttendeeCommand(attendanceContext.AttendeeId, request.TicketId), cancellationToken);

                return result.Match(Results.NoContent, ApiResults.Problem);
            })
            .RequireAuthorization(Permissions.CheckInTicket)
            .WithTags(Tags.Attendees);
    }

    internal sealed class Request
    {
        public Guid TicketId { get; init; }
    }
}
