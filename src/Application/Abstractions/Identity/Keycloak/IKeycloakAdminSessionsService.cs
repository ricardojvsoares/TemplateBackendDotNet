using Application.Abstractions.Identity.Keycloak.Sessions.Requests;
using Application.Abstractions.Identity.Keycloak.Sessions.Responses;
using ErrorOr;

namespace Application.Abstractions.Identity.Keycloak;

public interface IKeycloakAdminSessionsService
{
    Task<ErrorOr<IReadOnlyCollection<KeycloakSessionResponse>>> GetUserSessionsAsync(
        GetUserSessionsRequest request,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<Success>> RevokeSessionAsync(
        RevokeUserSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<Success>> RevokeAllUserSessionsAsync(
        RevokeAllUserSessionsRequest request,
        CancellationToken cancellationToken = default);
}
