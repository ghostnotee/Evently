using AwesomeAssertions;
using Evently.Common.Domain;
using Evently.Modules.Events.Application.Events.PublishEvent;
using Evently.Modules.Events.Application.Events.RescheduleEvent;
using Evently.Modules.Events.Domain.Events;
using Evently.Modules.Events.IntegrationTests.Abstractions;

namespace Evently.Modules.Events.IntegrationTests.Events;

public class RescheduleEventTests : BaseIntegrationTest
{
    public RescheduleEventTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }


    [Fact]
    public async Task Should_ReturnFailure_WhenEventDoesNotExist()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var command = new PublishEventCommand(eventId);

        // Act
        Result result = await SendCommand(command);

        // Assert
        result.Error.Should().Be(EventErrors.NotFound(eventId));
    }

    [Fact]
    public async Task Should_ReturnFailure_WhenStartDateIsInPast()
    {
        // Arrange
        Guid categoryId = await this.CreateCategoryAsync(Faker.Music.Genre());
        Guid eventId = await this.CreateEventAsync(categoryId);

        DateTime startsAtUtc = DateTime.UtcNow.AddMinutes(-5);

        var command = new RescheduleEventCommand(eventId, startsAtUtc, null);

        // Act
        Result result = await SendCommand(command);

        // Assert
        result.Error.Should().Be(EventErrors.StartDateInPast);
    }

    [Fact]
    public async Task Should_ReturnSuccess_WhenEventIsRescheduled()
    {
        // Arrange
        Guid categoryId = await this.CreateCategoryAsync(Faker.Music.Genre());
        Guid eventId = await this.CreateEventAsync(categoryId);

        var command = new RescheduleEventCommand(eventId, DateTime.UtcNow.AddMinutes(10), null);

        // Act
        Result result = await SendCommand(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
