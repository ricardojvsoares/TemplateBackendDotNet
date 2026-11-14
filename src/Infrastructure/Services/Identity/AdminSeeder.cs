using Application.Abstractions.Identity;
using Application.Settings;
using Domain.Authorization;
using Domain.Features.Users;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Identity;

internal sealed class AdminSeeder(
    IUserRepository users,
    IPasswordHasher passwords,
    IOptions<AuthServiceSettings> options,
    ILogger<AdminSeeder> logger) : IAdminSeeder
{
    private readonly AuthServiceSettings _settings = options.Value;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.AdminEmail)
            || string.IsNullOrWhiteSpace(_settings.AdminFirstName)
            || string.IsNullOrWhiteSpace(_settings.AdminLastName))
        {
            throw new InvalidOperationException(
                "AUTH_ADMIN_EMAIL, AUTH_ADMIN_FIRST_NAME and AUTH_ADMIN_LAST_NAME must be set.");
        }

        ErrorOr<Success> password = UserRules.ValidatePassword(_settings.AdminPassword);
        if (password.IsError)
        {
            throw new InvalidOperationException(password.FirstError.Description);
        }

        string email = UserRules.NormalizeEmail(_settings.AdminEmail);
        ErrorOr<User> existing = await users.GetByEmailAsync(email, cancellationToken);
        if (!existing.IsError)
        {
            return;
        }

        if (existing.FirstError.Type != ErrorType.NotFound)
        {
            throw new InvalidOperationException(existing.FirstError.Description);
        }

        User admin = User.Create(
            _settings.AdminFirstName.Trim(),
            _settings.AdminLastName.Trim(),
            email,
            passwords.Hash(_settings.AdminPassword),
            GlobalRoles.Admin,
            [],
            Guid.Empty);

        ErrorOr<Guid> created = await users.CreateAsync(admin, cancellationToken);
        if (created.IsError)
        {
            if (created.FirstError.Code == "User.AlreadyExists")
            {
                return;
            }

            throw new InvalidOperationException(created.FirstError.Description);
        }

        logger.LogInformation("Seeded admin user {Email}", email);
    }
}
