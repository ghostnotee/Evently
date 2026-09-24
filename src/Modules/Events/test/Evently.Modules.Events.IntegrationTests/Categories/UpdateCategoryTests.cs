using AwesomeAssertions;
using Evently.Common.Domain;
using Evently.Modules.Events.Application.Categories.UpdateCategory;
using Evently.Modules.Events.Domain.Categories;
using Evently.Modules.Events.IntegrationTests.Abstractions;

namespace Evently.Modules.Events.IntegrationTests.Categories;

public class UpdateCategoryTests : BaseIntegrationTest
{

    public static readonly TheoryData<UpdateCategoryCommand> InvalidCommands = new()
    {
        new UpdateCategoryCommand(Guid.Empty, Faker.Music.Genre()),
        new UpdateCategoryCommand(Guid.NewGuid(), string.Empty)
    };

    public UpdateCategoryTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task Should_ReturnFailure_WhenCommandIsNotValid(UpdateCategoryCommand command)
    {
        // Act
        Result result = await SendCommand(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Should_ReturnFailure_WhenCategoryDoesNotExist()
    {
        // Arrange
        var command = new UpdateCategoryCommand(Guid.NewGuid(), Faker.Music.Genre());

        // Act
        Result result = await SendCommand(command);

        // Assert
        result.Error.Should().Be(CategoryErrors.NotFound(command.CategoryId));
    }

    [Fact]
    public async Task Should_UpdateCategory_WhenCategoryExists()
    {
        // Arrange
        Guid categoryId = await this.CreateCategoryAsync(Faker.Music.Genre());

        var command = new UpdateCategoryCommand(categoryId, Faker.Music.Genre());

        // Act
        Result result = await SendCommand(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
