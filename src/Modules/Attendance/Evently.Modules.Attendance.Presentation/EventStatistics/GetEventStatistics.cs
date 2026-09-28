using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Common.Presentation.Endpoints;
using Evently.Common.Presentation.Results;
using Evently.Modules.Attendance.Application.EventStatistics.GetEventStatistics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Evently.Modules.Attendance.Presentation.EventStatistics;

internal sealed class GetEventStatistics : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("event-statistics/{id}", async (
                Guid id,
                IQueryHandler<GetEventStatisticsQuery, EventStatisticsResponse> handler,
                CancellationToken cancellationToken) =>
            {
                Result<EventStatisticsResponse> result = await handler.HandleAsync(new GetEventStatisticsQuery(id), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .RequireAuthorization(Permissions.GetEventStatistics)
            .WithTags(Tags.EventStatistics);
    }
}
