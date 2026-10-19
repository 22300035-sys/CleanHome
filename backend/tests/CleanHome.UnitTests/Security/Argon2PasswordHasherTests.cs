using CleanHome.Infrastructure.Security;

namespace CleanHome.UnitTests.Security;

public sealed class Argon2PasswordHasherTests
{
    private readonly Argon2PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ProducesArgon2idHash()
    {
        const string password = "ClaveSegura!2026";

        var hash = _hasher.HashPassword(password);

        Assert.StartsWith("$argon2id$v=19$", hash);
        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void VerifyPassword_AcceptsCorrectPassword()
    {
        const string password = "ClaveSegura!2026";

        var hash = _hasher.HashPassword(password);

        Assert.True(_hasher.VerifyPassword(password, hash));
    }

    [Fact]
    public void VerifyPassword_RejectsIncorrectPassword()
    {
        var hash = _hasher.HashPassword("ClaveCorrecta!2026");

        Assert.False(
            _hasher.VerifyPassword("ClaveIncorrecta!2026", hash));
    }

    [Fact]
    public void HashPassword_UsesDifferentSaltEachTime()
    {
        const string password = "MismaClave!2026";

        var firstHash = _hasher.HashPassword(password);
        var secondHash = _hasher.HashPassword(password);

        Assert.NotEqual(firstHash, secondHash);

        Assert.True(_hasher.VerifyPassword(password, firstHash));
        Assert.True(_hasher.VerifyPassword(password, secondHash));
    }

    [Fact]
    public void VerifyPassword_RejectsMalformedHash()
    {
        Assert.False(
            _hasher.VerifyPassword(
                "ClaveSegura!2026",
                "hash-invalido"));
    }

    [Fact]
    public void VerifyPassword_RejectsExcessiveMemoryParameters()
    {
        var hash = _hasher.HashPassword("ClaveSegura!2026");

        var alteredHash = hash.Replace(
            "m=19456",
            "m=999999999",
            StringComparison.Ordinal);

        Assert.False(
            _hasher.VerifyPassword(
                "ClaveSegura!2026",
                alteredHash));
    }

    [Fact]
    public void NeedsRehash_ReturnsFalseForCurrentParameters()
    {
        var hash = _hasher.HashPassword("ClaveSegura!2026");

        Assert.False(_hasher.NeedsRehash(hash));
    }

    [Fact]
    public void HashPassword_RejectsEmptyPassword()
    {
        Assert.Throws<ArgumentException>(
            () => _hasher.HashPassword(""));
    }
}
