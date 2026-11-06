using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Policies;

public sealed record PermissionRequirement(
    string Permission)
    : IAuthorizationRequirement;
