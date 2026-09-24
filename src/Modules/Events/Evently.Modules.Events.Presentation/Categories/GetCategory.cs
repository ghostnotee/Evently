using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Common.Presentation.Endpoints;
using Evently.Common.Presentation.Results;
using Evently.Modules.Events.Application.Categories.GetCategory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Evently.Modules.Events.Presentation.Categories;

internal sealed class GetCategory : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("categories/{id}", async (
                Guid id,
                IQueryHandler<GetCategoryQuery, CategoryResponse> handler,
                CancellationToken cancellationToken) =>
            {
                Result<CategoryResponse> result = await handler.HandleAsync(new GetCategoryQuery(id), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .RequireAuthorization(Permissions.GetCategories)
            .WithTags(Tags.Categories);
    }
}
