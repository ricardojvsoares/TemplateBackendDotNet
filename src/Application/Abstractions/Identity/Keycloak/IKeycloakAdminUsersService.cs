using Application.Abstractions.Identity.Keycloak.Users.Requests;
using Application.Abstractions.Identity.Keycloak.Users.Responses;
using ErrorOr;

namespace Application.Abstractions.Identity.Keycloak;

public interface IKeycloakAdminUsersService
{
    Task<ErrorOr<IReadOnlyCollection<KeycloakUserResponse>>> SearchAsync(
        SearchKeycloakUsersRequest request,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<KeycloakUserResponse?>> GetByIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<Created>> CreateAsync(
        CreateKeycloakUserRequest request,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<Updated>> UpdateAsync(
        UpdateKeycloakUserRequest request,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<Deleted>> DeleteAsync(
        DeleteKeycloakUserRequest request,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<Success>> ResetPasswordAsync(
        ResetKeycloakUserPasswordRequest request,
        CancellationToken cancellationToken = default);
}
