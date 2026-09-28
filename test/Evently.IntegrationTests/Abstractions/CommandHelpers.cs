using AwesomeAssertions;
using Bogus;
using Evently.Common.Domain;
using Evently.Modules.Ticketing.Application.Events.CreateEvent;

namespace Evently.IntegrationTests.Abstractions;

internal static class CommandHelpers
{
    internal static async Task CreateEventAsync(
        this BaseIntegrationTest test,
        Guid eventId,
        Guid ticketTypeId,
        decimal quantity)
    {
        var faker = new Faker();

        var ticketType = new CreateEventCommand.TicketTypeRequest(
            ticketTypeId,
            eventId,
            faker.Music.Genre(),
            faker.Random.Decimal(),
            "USD",
            quantity);

        Result result = await test.SendCommand(new CreateEventCommand(
            eventId,
            faker.Music.Genre(),
            faker.Music.Genre(),
            faker.Address.FullAddress(),
            DateTime.UtcNow,
            null,
            [ticketType]));

        result.IsSuccess.Should().BeTrue();
    }
}
