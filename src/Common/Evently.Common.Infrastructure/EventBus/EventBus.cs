using Evently.Common.Application.EventBus;
using Wolverine;

namespace Evently.Common.Infrastructure.EventBus;

internal sealed class EventBus(IMessageBus bus) : IEventBus
{
    public async Task PublishAsync<T>(
        T integrationEvent,
        CancellationToken cancellationToken = default) where T : IIntegrationEvent
    {
        await bus.PublishAsync(integrationEvent);
    }
}
