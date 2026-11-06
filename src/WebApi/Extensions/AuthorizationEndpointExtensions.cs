using Infrastructure.Policies;

namespace WebApi.Extensions;

internal static class AuthorizationEndpointExtensions
{
    public static TBuilder RequirePermission<TBuilder>(
        this TBuilder builder,
        string permission)
        where TBuilder
        : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(
                new PermissionRequirement(permission));
        });
    }
}
