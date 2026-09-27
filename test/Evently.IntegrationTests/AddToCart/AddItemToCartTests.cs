using AwesomeAssertions;
using Evently.Common.Domain;
using Evently.IntegrationTests.Abstractions;
using Evently.Modules.Ticketing.Application.Carts.AddItemToCart;
using Evently.Modules.Ticketing.Application.Customers.GetCustomer;
using Evently.Modules.Users.Application.Users.RegisterUser;

namespace Evently.IntegrationTests.AddToCart;

public sealed class AddItemToCartTests : BaseIntegrationTest
{
    private const decimal Quantity = 10;

    public AddItemToCartTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Customer_ShouldBeAbleTo_AddItemToCart()
    {
        // Register user
        var command = new RegisterUserCommand(
            Faker.Internet.Email(),
            Faker.Internet.Password(6),
            Faker.Name.FirstName(),
            Faker.Name.LastName());

        Result<Guid> userResult = await SendCommand<RegisterUserCommand, Guid>(command);

        userResult.IsSuccess.Should().BeTrue();

        // Get customer
        Result<CustomerResponse> customerResult = await Poller.WaitAsync(
            TimeSpan.FromSeconds(15),
            async () =>
            {
                var query = new GetCustomerQuery(userResult.Value);

                Result<CustomerResponse> customerResult =
                    await SendQuery<GetCustomerQuery, CustomerResponse>(query);

                return customerResult;
            });

        customerResult.IsSuccess.Should().BeTrue();

        // Add item to cart
        CustomerResponse customer = customerResult.Value;
        var ticketTypeId = Guid.NewGuid();

        await this.CreateEventAsync(Guid.NewGuid(), ticketTypeId, Quantity);

        Result result = await SendCommand(new AddItemToCartCommand(customer.Id, ticketTypeId, Quantity));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
