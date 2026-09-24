using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Bogus;
using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Modules.Users.Infrastructure.Database;
using Evently.Modules.Users.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Evently.Modules.Users.IntegrationTests.Abstractions;

[Collection(nameof(IntegrationTestCollection))]
public abstract class BaseIntegrationTest : IDisposable
{
    protected static readonly Faker Faker = new();
    protected readonly UsersDbContext DbContext;
    protected readonly HttpClient HttpClient;
    private readonly KeyCloakOptions _options;
    private readonly IServiceScope _scope;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        _scope = factory.Services.CreateScope();
        HttpClient = factory.CreateClient();
        DbContext = _scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        _options = factory.Services.GetRequiredService<IOptions<KeyCloakOptions>>().Value;
    }

    public void Dispose()
    {
        _scope.Dispose();
    }

    protected async Task<Result<TResult>> SendCommand<TCommand, TResult>(TCommand command) where TCommand : ICommand<TResult>
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
            DELETE FROM users.inbox_message_consumers;
            DELETE FROM users.inbox_messages;
            DELETE FROM users.outbox_message_consumers;
            DELETE FROM users.outbox_messages;
            DELETE FROM users.users;
            DELETE FROM users.user_roles;
            """);
    }

    protected async Task<string> GetAccessTokenAsync(string email, string password)
    {
        using var client = new HttpClient();

        var authRequestParameters = new KeyValuePair<string, string>[]
        {
            new("client_id", _options.PublicClientId), new("scope", "openid"), new("grant_type", "password"), new("username", email),
            new("password", password)
        };

        using var authRequestContent = new FormUrlEncodedContent(authRequestParameters);

        using var authRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.TokenUrl));
        authRequest.Content = authRequestContent;

        using HttpResponseMessage authorizationResponse = await client.SendAsync(authRequest);

        authorizationResponse.EnsureSuccessStatusCode();

        AuthToken authToken = await authorizationResponse.Content.ReadFromJsonAsync<AuthToken>();

        return authToken!.AccessToken;
    }

    internal sealed class AuthToken
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; }
    }
}
