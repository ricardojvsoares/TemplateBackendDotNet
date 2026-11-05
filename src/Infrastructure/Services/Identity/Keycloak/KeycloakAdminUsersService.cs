using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Application.Abstractions.Identity.Keycloak;
using Application.Abstractions.Identity.Keycloak.Users.Requests;
using Application.Abstractions.Identity.Keycloak.Users.Responses;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Identity.Keycloak;

internal sealed class KeycloakAdminUsersService(
    HttpClient httpClient,
    ILogger<KeycloakAdminUsersService> logger)
    : IKeycloakAdminUsersService
{
    private const string Action = "UPDATE_PASSWORD";

    public async Task<ErrorOr<IReadOnlyCollection<KeycloakUserResponse>>> SearchAsync(
        SearchKeycloakUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new List<string>
            {
                $"first={request.First.ToString(CultureInfo.InvariantCulture)}",
                $"max={request.Max.ToString(CultureInfo.InvariantCulture)}"
            };

            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                query.Add($"search={Uri.EscapeDataString(request.Query)}");
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                query.Add($"email={Uri.EscapeDataString(request.Email)}");
                query.Add("exact=true");
            }

            ErrorOr<List<KeycloakUserDto>> result = await GetUsersAsync(
                new Uri($"users?{string.Join('&', query)}", UriKind.Relative),
                "search users",
                cancellationToken);

            if (result.IsError)
            {
                return result.Errors;
            }

            return result.Value.ConvertAll(Map);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching Keycloak users");
            return KeycloakErrors.Unexpected("search users");
        }
    }

    public async Task<ErrorOr<KeycloakUserResponse?>> GetByIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(
                new Uri($"users/{Uri.EscapeDataString(userId)}", UriKind.Relative),
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                KeycloakUserResponse? none = null;
                return none;
            }

            if (!response.IsSuccessStatusCode)
            {
                return await KeycloakErrors.FromResponseAsync(response, logger, "get user", cancellationToken);
            }

            KeycloakUserDto? dto = await response.Content.ReadFromJsonAsync<KeycloakUserDto>(
                KeycloakJson.Options,
                cancellationToken);

            KeycloakUserResponse? mapped = dto is null ? null : Map(dto);
            return mapped;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting Keycloak user {UserId}", userId);
            return KeycloakErrors.Unexpected("get user");
        }
    }

    public async Task<ErrorOr<Created>> CreateAsync(
        CreateKeycloakUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var body = new KeycloakUserDto
            {
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Enabled = request.Enabled,
                EmailVerified = request.EmailVerified
            };

            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                new Uri("users", UriKind.Relative),
                body,
                KeycloakJson.Options,
                cancellationToken);

            return !response.IsSuccessStatusCode
                ? (ErrorOr<Created>)await KeycloakErrors.FromResponseAsync(response, logger, "create user", cancellationToken)
                : (ErrorOr<Created>)Result.Created;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating Keycloak user");
            return KeycloakErrors.Unexpected("create user");
        }
    }

    public async Task<ErrorOr<Updated>> UpdateAsync(
        UpdateKeycloakUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ErrorOr<KeycloakUserDto> found = await FindByEmailAsync(request.Email, cancellationToken);

            if (found.IsError)
            {
                return found.Errors;
            }

            var body = new KeycloakUserDto
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Enabled = request.Enabled
            };

            using HttpResponseMessage response = await httpClient.PutAsJsonAsync(
                new Uri($"users/{Uri.EscapeDataString(found.Value.Id!)}", UriKind.Relative),
                body,
                KeycloakJson.Options,
                cancellationToken);

            return !response.IsSuccessStatusCode
                ? (ErrorOr<Updated>)await KeycloakErrors.FromResponseAsync(response, logger, "update user", cancellationToken)
                : (ErrorOr<Updated>)Result.Updated;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating Keycloak user");
            return KeycloakErrors.Unexpected("update user");
        }
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(
        DeleteKeycloakUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ErrorOr<KeycloakUserDto> found = await FindByEmailAsync(request.UserEmail, cancellationToken);

            if (found.IsError)
            {
                return found.Errors;
            }

            using HttpResponseMessage response = await httpClient.DeleteAsync(
                new Uri($"users/{Uri.EscapeDataString(found.Value.Id!)}", UriKind.Relative),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await KeycloakErrors.FromResponseAsync(response, logger, "delete user", cancellationToken);
            }

            return Result.Deleted;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting Keycloak user");
            return KeycloakErrors.Unexpected("delete user");
        }
    }

    public async Task<ErrorOr<Success>> ResetPasswordAsync(
        ResetKeycloakUserPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ErrorOr<KeycloakUserDto> found = await FindByEmailAsync(request.UserEmail, cancellationToken);

            if (found.IsError)
            {
                return found.Errors;
            }
            using HttpResponseMessage response = await httpClient.PutAsJsonAsync(
                new Uri($"users/{Uri.EscapeDataString(found.Value.Id!)}/execute-actions-email", UriKind.Relative),
                new[] { Action },
                KeycloakJson.Options,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await KeycloakErrors.FromResponseAsync(response, logger, "reset password", cancellationToken);
            }

            return Result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting Keycloak user password");
            return KeycloakErrors.Unexpected("reset password");
        }
    }

    private async Task<ErrorOr<List<KeycloakUserDto>>> GetUsersAsync(
        Uri relativeUri,
        string operation,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(relativeUri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return await KeycloakErrors.FromResponseAsync(response, logger, operation, cancellationToken);
        }

        List<KeycloakUserDto>? users = await response.Content.ReadFromJsonAsync<List<KeycloakUserDto>>(
            KeycloakJson.Options,
            cancellationToken);

        return users ?? [];
    }

    private async Task<ErrorOr<KeycloakUserDto>> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        ErrorOr<List<KeycloakUserDto>> result = await GetUsersAsync(
            new Uri($"users?email={Uri.EscapeDataString(email)}&exact=true", UriKind.Relative),
            "find user by email",
            cancellationToken);

        if (result.IsError)
        {
            return result.Errors;
        }

        // Re-check client side in case the server ignores "exact".
        KeycloakUserDto? user = result.Value.Find(
            u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

        return user is null || string.IsNullOrEmpty(user.Id)
            ? KeycloakErrors.UserNotFound(email)
            : user;
    }

    private static KeycloakUserResponse Map(KeycloakUserDto dto) => new()
    {
        Id = dto.Id ?? string.Empty,
        Username = dto.Username ?? string.Empty,
        Email = dto.Email ?? string.Empty,
        FirstName = dto.FirstName ?? string.Empty,
        LastName = dto.LastName ?? string.Empty,
        Enabled = dto.Enabled,
        EmailVerified = dto.EmailVerified
    };
}
