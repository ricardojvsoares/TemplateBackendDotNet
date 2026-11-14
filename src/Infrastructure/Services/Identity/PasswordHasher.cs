using Application.Abstractions.Identity;
using Domain.Features.Users;
using IdentityPasswordHasher = Microsoft.AspNetCore.Identity.PasswordHasher<Domain.Features.Users.User>;
using PasswordVerificationResult = Microsoft.AspNetCore.Identity.PasswordVerificationResult;

namespace Infrastructure.Services.Identity;

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly IdentityPasswordHasher _hasher = new();
    private readonly string _dummyHash;

    public PasswordHasher()
    {
        _dummyHash = _hasher.HashPassword(new User(), Guid.NewGuid().ToString("N"));
    }

    public string Hash(string password) => _hasher.HashPassword(new User(), password);

    public bool Verify(string passwordHash, string password)
    {
        bool hasHash = !string.IsNullOrEmpty(passwordHash);
        string hash = hasHash ? passwordHash : _dummyHash;
        PasswordVerificationResult result = _hasher.VerifyHashedPassword(new User(), hash, password);
        return hasHash && result != PasswordVerificationResult.Failed;
    }
}
