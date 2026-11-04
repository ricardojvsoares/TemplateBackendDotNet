using Application.Abstractions.Identity.Keycloak.Clients.Responses;
using ErrorOr;

namespace Application.Abstractions.Identity.Keycloak;

public interface IKeycloakAdminClientsService
{
    Task<ErrorOr<KeycloakClient>> GetAsync(
        CancellationToken cancellationToken = default);
}
