using System.Security.Claims;

namespace Application.Abstractions.Identity;

public interface IUserContext
{
    bool IsAuthenticated { get; }
    string UserEmail { get; }
    Guid UserId { get; }
    IEnumerable<Claim> Roles { get; }
    string? LanguageCode { get; set; }
    long LanguageId { get; set; }
}