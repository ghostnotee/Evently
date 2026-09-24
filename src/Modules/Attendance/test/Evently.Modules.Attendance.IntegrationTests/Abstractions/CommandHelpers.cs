using AwesomeAssertions;
using Bogus;
using Evently.Common.Domain;
using Evently.Modules.Attendance.Application.Attendees.CreateAttendee;
using Evently.Modules.Attendance.Application.Events.CreateEvent;
using Evently.Modules.Attendance.Application.Tickets.CreateTicket;

namespace Evently.Modules.Attendance.IntegrationTests.Abstractions;

internal static class CommandHelpers
{
    internal static async Task<Guid> CreateAttendeeAsync(this BaseIntegrationTest test, Guid attendeeId)
    {
        var faker = new Faker();
        Result result = await test.SendCommand(
            new CreateAttendeeCommand(
                attendeeId,
                faker.Internet.Email(),
                faker.Name.FirstName(),
                faker.Name.LastName()));

        result.IsSuccess.Should().BeTrue();

        return attendeeId;
    }

    internal static async Task<Guid> CreateTicketAsync(
        this BaseIntegrationTest test,
        Guid ticketId,
        Guid attendeeId,
        Guid eventId)
    {
        Result result = await test.SendCommand(
            new CreateTicketCommand(
                ticketId,
                attendeeId,
                eventId,
                Ulid.NewUlid().ToString()));

        result.IsSuccess.Should().BeTrue();

        return ticketId;
    }

    internal static async Task<Guid> CreateEventAsync(this BaseIntegrationTest test, Guid eventId)
    {
        var faker = new Faker();
        Result result = await test.SendCommand(
            new CreateEventCommand(
                eventId,
                faker.Music.Genre(),
                faker.Music.Genre(),
                faker.Address.StreetAddress(),
                DateTime.UtcNow.AddMinutes(10),
                null));

        result.IsSuccess.Should().BeTrue();

        return eventId;
    }
}
