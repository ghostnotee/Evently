using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Common.Presentation.Endpoints;
using Evently.Common.Presentation.Results;
using Evently.Modules.Events.Application.Categories.CreateCategory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Evently.Modules.Events.Presentation.Categories;

internal sealed class CreateCategory : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("categories", async (
                Request request,
                ICommandHandler<CreateCategoryCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                Result<Guid> result = await handler.HandleAsync(new CreateCategoryCommand(request.Name), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .RequireAuthorization(Permissions.ModifyCategories)
            .WithTags(Tags.Categories);
    }

    internal sealed class Request
    {
        public string Name { get; init; }
    }
}
