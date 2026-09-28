using AwesomeAssertions;
using Evently.Common.Domain;
using Evently.IntegrationTests.Abstractions;
using Evently.Modules.Events.Application.Categories.CreateCategory;
using Evently.Modules.Events.Application.Events.CancelEvent;
using Evently.Modules.Events.Application.Events.CreateEvent;
using Evently.Modules.Events.Application.Events.PublishEvent;
using Evently.Modules.Events.Application.TicketTypes.CreateTicketType;
using Evently.Modules.Ticketing.Application.Carts.AddItemToCart;
using Evently.Modules.Ticketing.Application.Customers.GetCustomer;
using Evently.Modules.Ticketing.Application.Orders.CreateOrder;
using Evently.Modules.Ticketing.Application.Orders.GetOrders;
using Evently.Modules.Ticketing.Domain.Orders;
using Evently.Modules.Users.Application.Users.RegisterUser;

namespace Evently.IntegrationTests.CancelEvent;

public sealed class CancelEventSagaTests : BaseIntegrationTest
{
    public CancelEventSagaTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CancelEvent_ShouldComplete_FullSagaFlow_RefundingPaymentsAndArchivingTickets()
    {
        // -----------------------------------------------------------------------
        // 1. Create an event with a ticket type in the Events module and publish it.
        //    Publishing fires an integration event that propagates the event and
        //    its ticket types into the Ticketing module asynchronously.
        // -----------------------------------------------------------------------
        Result<Guid> categoryResult = await SendCommand<CreateCategoryCommand, Guid>(
            new CreateCategoryCommand(Faker.Music.Genre()));

        categoryResult.IsSuccess.Should().BeTrue();

        Result<Guid> eventResult = await SendCommand<CreateEventCommand, Guid>(
            new CreateEventCommand(
                categoryResult.Value,
                Faker.Music.Genre(),
                Faker.Lorem.Sentence(),
                Faker.Address.FullAddress(),
                DateTime.UtcNow.AddDays(30),
                null));

        eventResult.IsSuccess.Should().BeTrue();
        Guid eventId = eventResult.Value;

        Result<Guid> ticketTypeResult = await SendCommand<CreateTicketTypeCommand, Guid>(
            new CreateTicketTypeCommand(
                eventId,
                Faker.Commerce.ProductName(),
                50m,
                "USD",
                100m));

        ticketTypeResult.IsSuccess.Should().BeTrue();
        Guid ticketTypeId = ticketTypeResult.Value;

        Result publishResult = await SendCommand(new PublishEventCommand(eventId));
        publishResult.IsSuccess.Should().BeTrue();

        // -----------------------------------------------------------------------
        // 2. Register a user and wait for the customer record to appear in the
        //    Ticketing module (propagated via integration event).
        // -----------------------------------------------------------------------
        Result<Guid> userResult = await SendCommand<RegisterUserCommand, Guid>(
            new RegisterUserCommand(
                Faker.Internet.Email(),
                Faker.Internet.Password(6),
                Faker.Name.FirstName(),
                Faker.Name.LastName()));

        userResult.IsSuccess.Should().BeTrue();

        Result<CustomerResponse> customerResult = await Poller.WaitAsync(
            TimeSpan.FromSeconds(15),
            async () => await SendQuery<GetCustomerQuery, CustomerResponse>(
                new GetCustomerQuery(userResult.Value)));

        customerResult.IsSuccess.Should().BeTrue();
        Guid customerId = customerResult.Value.Id;

        // -----------------------------------------------------------------------
        // 3. Add a ticket to the cart then place the order.
        //    The Ticketing module creates the event from the EventPublishedIntegrationEvent,
        //    so we poll until the cart item is accepted (i.e. the event has arrived).
        // -----------------------------------------------------------------------
        Result<bool> addToCartResult = await Poller.WaitAsync(
            TimeSpan.FromSeconds(15),
            async () =>
            {
                Result result = await SendCommand(
                    new AddItemToCartCommand(customerId, ticketTypeId, 1));

                return result.IsSuccess
                    ? Result.Success(true)
                    : Result.Failure<bool>(result.Error);
            });

        addToCartResult.IsSuccess.Should().BeTrue();

        Result orderResult = await SendCommand(new CreateOrderCommand(customerId));
        orderResult.IsSuccess.Should().BeTrue();

        // Wait until at least one order exists for the customer (the order was persisted).
        Result<IReadOnlyCollection<OrderResponse>> ordersResult = await Poller.WaitAsync(
            TimeSpan.FromSeconds(15),
            async () =>
            {
                Result<IReadOnlyCollection<OrderResponse>> result =
                    await SendQuery<GetOrdersQuery, IReadOnlyCollection<OrderResponse>>(
                        new GetOrdersQuery(customerId));

                if (result.IsFailure || !result.Value.Any())
                {
                    return Result.Failure<IReadOnlyCollection<OrderResponse>>(
                        Error.Failure("Orders.NotFound", "No orders found yet"));
                }

                return result;
            });

        ordersResult.IsSuccess.Should().BeTrue();
        Guid orderId = ordersResult.Value.First().Id;

        // -----------------------------------------------------------------------
        // 4. Act – cancel the event.
        //    This triggers the CancelEventSaga:
        //      EventCanceled → CancellationStarted → EventCancellationStartedIntegrationEvent
        //      → Ticketing: RefundPayments + ArchiveTickets
        //      → EventPaymentsRefunded + EventTicketsArchived integration events
        //      → Saga finalises → EventCancellationCompletedIntegrationEvent
        // -----------------------------------------------------------------------
        Result cancelResult = await SendCommand(
            new CancelEventCommand(eventId));

        cancelResult.IsSuccess.Should().BeTrue();

        // -----------------------------------------------------------------------
        // 5. Assert – poll until the order is Refunded.
        //    An order with status Refunded proves that the saga completed the
        //    RefundPayments arm.  Both arms must complete before the saga
        //    finalises, so reaching this state confirms the full saga flow.
        // -----------------------------------------------------------------------
        Result<IReadOnlyCollection<OrderResponse>> refundedOrdersResult = await Poller.WaitAsync(
            TimeSpan.FromSeconds(30),
            async () =>
            {
                Result<IReadOnlyCollection<OrderResponse>> result =
                    await SendQuery<GetOrdersQuery, IReadOnlyCollection<OrderResponse>>(
                        new GetOrdersQuery(customerId));

                if (result.IsFailure)
                {
                    return result;
                }

                OrderResponse? refundedOrder = result.Value
                    .FirstOrDefault(o => o.Id == orderId && o.Status == OrderStatus.Refunded);

                if (refundedOrder is null)
                {
                    return Result.Failure<IReadOnlyCollection<OrderResponse>>(
                        Error.Failure("Order.NotRefunded", "Order has not been refunded yet"));
                }

                return result;
            });

        refundedOrdersResult.IsSuccess.Should().BeTrue();

        OrderResponse finalOrder = refundedOrdersResult.Value.Single(o => o.Id == orderId);
        finalOrder.Status.Should().Be(OrderStatus.Refunded);
    }
}
