using AwesomeAssertions;
using Evently.Common.Domain;
using Evently.Modules.Events.Application.TicketTypes.GetTicketType;
using Evently.Modules.Events.Application.TicketTypes.GetTicketTypes;
using Evently.Modules.Events.IntegrationTests.Abstractions;

namespace Evently.Modules.Events.IntegrationTests.TicketTypes;

public class GetTicketTypesTests : BaseIntegrationTest
{
    public GetTicketTypesTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnFailure_WhenTicketTypesDoNotExist()
    {
        // Arrange
        await CleanDatabaseAsync();

        var query = new GetTicketTypesQuery(Guid.NewGuid());

        // Act
        Result<IReadOnlyCollection<TicketTypeResponse>> result =
            await SendQuery<GetTicketTypesQuery, IReadOnlyCollection<TicketTypeResponse>>(query);

        // Assert
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ReturnTicketTypes_WhenTicketTypesExists()
    {
        // Arrange
        await CleanDatabaseAsync();

        Guid categoryId = await this.CreateCategoryAsync(Faker.Music.Genre());
        Guid eventId = await this.CreateEventAsync(categoryId);

        await this.CreateTicketTypeAsync(eventId);
        await this.CreateTicketTypeAsync(eventId);

        var query = new GetTicketTypesQuery(eventId);

        // Act
        Result<IReadOnlyCollection<TicketTypeResponse>> result =
            await SendQuery<GetTicketTypesQuery, IReadOnlyCollection<TicketTypeResponse>>(query);

        // Assert
        result.Value.Should().HaveCount(2);
    }
}
