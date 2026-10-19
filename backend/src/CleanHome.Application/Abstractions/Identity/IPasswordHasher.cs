namespace CleanHome.Application.Abstractions.Identity;

public interface IPasswordHasher
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string encodedHash);

    bool NeedsRehash(string encodedHash);
}
