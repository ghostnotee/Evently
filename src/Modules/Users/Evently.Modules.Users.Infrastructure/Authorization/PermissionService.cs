using Evently.Common.Application.Authorization;
using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Evently.Modules.Users.Application.Users.GetUserPermissions;

namespace Evently.Modules.Users.Infrastructure.Authorization;

internal sealed class PermissionService(IQueryHandler<GetUserPermissionsQuery, PermissionsResponse> handler) : IPermissionService
{
    public async Task<Result<PermissionsResponse>> GetUserPermissionsAsync(string identityId)
    {
        return await handler.HandleAsync(new GetUserPermissionsQuery(identityId), CancellationToken.None);
    }
}
