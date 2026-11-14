using System.Globalization;
using System.Security.Claims;
using Application.Abstractions.Identity;
using Domain.Authorization;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services.Identity;

public sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public string UserEmail => Principal?.FindFirstValue("email") ?? string.Empty;

    public Guid UserId => Guid.TryParse(Principal?.FindFirstValue("sub"), out Guid id) ? id : Guid.Empty;

    public string? TokenId => Principal?.FindFirstValue("jti");

    public DateTime? TokenExpiresAt
    {
        get
        {
            if (httpContextAccessor.HttpContext?.Items.TryGetValue(AuthTokenItems.ExpiresAt, out object? stored) == true
                && stored is DateTime expiresAt)
            {
                return AsUtc(expiresAt);
            }

            string? exp = Principal?.FindFirstValue("exp");
            if (!long.TryParse(exp, NumberStyles.Integer, CultureInfo.InvariantCulture, out long seconds))
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
        }
    }

    public string? Role
    {
        get
        {
            return Principal?
                .FindAll(AuthClaims.Role)
                .FirstOrDefault(claim => GlobalRoles.IsDefined(claim.Value))
                ?.Value;
        }
    }

    public IEnumerable<Claim> Roles => Principal?.FindAll(AuthClaims.Role) ?? [];

    public string? LanguageCode { get; set; }

    public long LanguageId { get; set; }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime()
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
