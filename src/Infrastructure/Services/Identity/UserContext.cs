using System.Security.Claims;
using Application.Abstractions.Identity;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services.Identity;

internal sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public string UserEmail => Principal?.FindFirstValue("email") ?? string.Empty;

    public Guid UserId => Guid.TryParse(Principal?.FindFirstValue("sub"), out Guid id) ? id : Guid.Empty;

    public IEnumerable<Claim> Roles => Principal?.FindAll(ClaimTypes.Role) ?? [];

    public string? LanguageCode { get; set; }

    public long LanguageId { get; set; }
}
