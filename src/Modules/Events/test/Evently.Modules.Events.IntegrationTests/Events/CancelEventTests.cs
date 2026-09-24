using AwesomeAssertions;
using Evently.Common.Domain;
using Evently.Modules.Events.Application.Events.CancelEvent;
using Evently.Modules.Events.Domain.Events;
using Evently.Modules.Events.IntegrationTests.Abstractions;
using NSubstitute;

namespace Evently.Modules.Events.IntegrationTests.Events;

public class CancelEventTests : BaseIntegrationTest
{
    public CancelEventTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnFailure_WhenEventDoesNotExist()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var command = new CancelEventCommand(eventId);

        // Act
        Result result = await SendCommand(command);

        // Assert
        result.Error.Should().Be(EventErrors.NotFound(eventId));
    }

    [Fact]
    public async Task Should_ReturnFailure_WhenEventAlreadyCanceled()
    {
        // Arrange
        Guid categoryId = await this.CreateCategoryAsync(Faker.Music.Genre());
        Guid eventId = await this.CreateEventAsync(categoryId);

        var command = new CancelEventCommand(eventId);

        await SendCommand(command);

        // Act
        Result result = await SendCommand(command);

        // Assert
        result.Error.Should().Be(EventErrors.AlreadyCanceled);
    }

    [Fact]
    public async Task Should_ReturnFailure_WhenEventAlreadyStarted()
    {
        // Arrange
        Guid categoryId = await this.CreateCategoryAsync(Faker.Music.Genre());

        Guid eventId = await this.CreateEventAsync(categoryId, DateTime.UtcNow.AddMinutes(5));

        Factory.DateTimeProviderMock.UtcNow.Returns(DateTime.UtcNow.AddMinutes(15));

        var command = new CancelEventCommand(eventId);

        // Act
        Result result = await SendCommand(command);

        // Assert
        result.Error.Should().Be(EventErrors.AlreadyStarted);

        Factory.DateTimeProviderMock.UtcNow.Returns(_ => DateTime.UtcNow);
    }

    [Fact]
    public async Task Should_ReturnSuccess_WhenEventIsCanceled()
    {
        // Arrange
        Guid categoryId = await this.CreateCategoryAsync(Faker.Music.Genre());
        Guid eventId = await this.CreateEventAsync(categoryId);

        var command = new CancelEventCommand(eventId);

        // Act
        Result result = await SendCommand(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
