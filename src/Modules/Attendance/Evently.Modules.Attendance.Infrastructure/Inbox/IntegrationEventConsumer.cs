using System.Data.Common;
using Dapper;
using Evently.Common.Application.Data;
using Evently.Common.Application.EventBus;
using Evently.Common.Infrastructure.Inbox;
using Evently.Common.Infrastructure.Serialization;
using Newtonsoft.Json;

namespace Evently.Modules.Attendance.Infrastructure.Inbox;

public abstract class IntegrationEventConsumer<TIntegrationEvent>(IDbConnectionFactory dbConnectionFactory)
    where TIntegrationEvent : IntegrationEvent
{
    public async Task Handle(TIntegrationEvent integrationEvent)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        var inboxMessage = new InboxMessage
        {
            Id = integrationEvent.Id,
            Type = integrationEvent.GetType().Name,
            Content = JsonConvert.SerializeObject(integrationEvent, SerializerSettings.Instance),
            OccurredOnUtc = integrationEvent.OccurredOnUtc
        };

        const string sql =
            """
            INSERT INTO attendance.inbox_messages(id, type, content, occurred_on_utc)
            VALUES (@Id, @Type, @Content::json, @OccurredOnUtc)
            ON CONFLICT DO NOTHING
            """;

        await connection.ExecuteAsync(sql, inboxMessage);
    }
}
