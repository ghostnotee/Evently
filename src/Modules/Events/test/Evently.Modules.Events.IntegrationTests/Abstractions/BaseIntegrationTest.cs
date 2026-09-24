using Bogus;
using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Modules.Events.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Evently.Modules.Events.IntegrationTests.Abstractions;

[Collection(nameof(IntegrationTestCollection))]
public abstract class BaseIntegrationTest : IDisposable
{
    protected static readonly Faker Faker = new();
    private readonly IServiceScope _scope;
    protected readonly IntegrationTestWebAppFactory Factory;
    protected readonly EventsDbContext DbContext;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        _scope = factory.Services.CreateScope();
        Factory = factory;
        DbContext = _scope.ServiceProvider.GetRequiredService<EventsDbContext>();
    }

    public async Task<Result<TResult>> SendCommand<TCommand, TResult>(TCommand command) where TCommand : ICommand<TResult>
    {
        ICommandHandler<TCommand, TResult> handler = _scope.ServiceProvider
            .GetRequiredService<ICommandHandler<TCommand, TResult>>();

        return await handler.HandleAsync(command, CancellationToken.None);
    }

    protected async Task<Result> SendCommand<TCommand>(TCommand command) where TCommand : ICommand
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
            DELETE FROM events.inbox_message_consumers;
            DELETE FROM events.inbox_messages;
            DELETE FROM events.outbox_message_consumers;
            DELETE FROM events.outbox_messages;
            DELETE FROM events.ticket_types;
            DELETE FROM events.events;
            DELETE FROM events.categories;
            """);
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
}
