using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Common.Presentation.Endpoints;
using Evently.Common.Presentation.Results;
using Evently.Modules.Events.Application.Events.GetEvent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Evently.Modules.Events.Presentation.Events;

internal sealed class GetEvent : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("events/{id}", async (
                Guid id,
                IQueryHandler<GetEventQuery, EventResponse> handler,
                CancellationToken cancellationToken) =>
            {
                Result<EventResponse> result = await handler.HandleAsync(new GetEventQuery(id), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .RequireAuthorization(Permissions.GetEvents)
            .WithTags(Tags.Events);
    }
}
