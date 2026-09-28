using Bogus;
using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Modules.Ticketing.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Evently.Modules.Ticketing.IntegrationTests.Abstractions;

[Collection(nameof(IntegrationTestCollection))]
public abstract class BaseIntegrationTest : IDisposable
{
    protected static readonly Faker Faker = new();
    private readonly IServiceScope _scope;
    protected readonly TicketingDbContext DbContext;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        _scope = factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<TicketingDbContext>();
    }

    protected async Task<Result<TResult>> SendCommand<TCommand, TResult>(TCommand command) where TCommand : ICommand<TResult>
    {
        ICommandHandler<TCommand, TResult> handler = _scope.ServiceProvider
            .GetRequiredService<ICommandHandler<TCommand, TResult>>();

        return await handler.HandleAsync(command, CancellationToken.None);
    }

    public async Task<Result> SendCommand<TCommand>(TCommand command) where TCommand : ICommand
    {
        ICommandHandler<TCommand> handler = _scope.ServiceProvider
            .GetRequiredService<ICommandHandler<TCommand>>();

        return await handler.HandleAsync(command, CancellationToken.None);
    }

    protected async Task<Result<TResult>> SendQuery<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>
    {
        IQueryHandler<TQuery, TResult> handler = _scope.ServiceProvider
            .GetRequiredService<IQueryHandler<TQuery, TResult>>();

        return await handler.HandleAsync(query, CancellationToken.None);
    }

    protected async Task CleanDatabaseAsync()
    {
        await DbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM ticketing.inbox_message_consumers;
            DELETE FROM ticketing.inbox_messages;
            DELETE FROM ticketing.outbox_message_consumers;
            DELETE FROM ticketing.outbox_messages;
            DELETE FROM ticketing.events;
            DELETE FROM ticketing.ticket_types;
            DELETE FROM ticketing.customers;
            DELETE FROM ticketing.orders;
            DELETE FROM ticketing.order_items;
            DELETE FROM ticketing.tickets;
            DELETE FROM ticketing.payments;
            """);
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
}
