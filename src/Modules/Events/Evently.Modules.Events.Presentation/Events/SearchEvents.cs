using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Common.Presentation.Endpoints;
using Evently.Common.Presentation.Results;
using Evently.Modules.Events.Application.Events.SearchEvents;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Evently.Modules.Events.Presentation.Events;

internal sealed class SearchEvents : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("events/search", async (
                IQueryHandler<SearchEventsQuery, SearchEventsResponse> handler,
                CancellationToken cancellationToken,
                Guid? categoryId,
                DateTime? startDate,
                DateTime? endDate,
                int page = 0,
                int pageSize = 15) =>
            {
                Result<SearchEventsResponse> result = await handler.HandleAsync(
                    new SearchEventsQuery(categoryId, startDate, endDate, page, pageSize), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .RequireAuthorization(Permissions.SearchEvents)
            .WithTags(Tags.Events);
    }
}
