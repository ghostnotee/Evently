using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Common.Presentation.Endpoints;
using Evently.Common.Presentation.Results;
using Evently.Modules.Events.Application.Categories.GetCategories;
using Evently.Modules.Events.Application.Categories.GetCategory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Evently.Modules.Events.Presentation.Categories;

internal sealed class GetCategories : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("categories", async (
                IQueryHandler<GetCategoriesQuery, IReadOnlyCollection<CategoryResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                Result<IReadOnlyCollection<CategoryResponse>> result = await handler.HandleAsync(new GetCategoriesQuery(), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .RequireAuthorization(Permissions.GetCategories)
            .WithTags(Tags.Categories);
    }
}
