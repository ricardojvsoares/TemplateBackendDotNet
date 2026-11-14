using Application.Abstractions.Identity;

namespace Application.Tests.Fakes;

internal sealed class FakePasswordHasher : IPasswordHasher
{
    private readonly List<string> _hashed = [];

    public IReadOnlyList<string> Hashed => _hashed;

    public string Hash(string password)
    {
        _hashed.Add(password);
        return "hashed:" + password;
    }

    public bool Verify(string passwordHash, string password) =>
        passwordHash == "hashed:" + password;
}
