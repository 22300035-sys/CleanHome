using System.Security.Cryptography;
using System.Text;
using CleanHome.Application.Abstractions.Identity;

namespace CleanHome.Infrastructure.Security.Tokens;

public sealed class SecureRefreshTokenGenerator
    : IRefreshTokenGenerator
{
    private const int TokenBytes = 64;

    public GeneratedRefreshToken Generate()
    {
        var randomBytes =
            RandomNumberGenerator.GetBytes(TokenBytes);

        var token = Convert.ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var hash = ComputeHash(token);

        return new GeneratedRefreshToken(
            token,
            hash);
    }

    public string ComputeHash(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException(
                "El refresh token es obligatorio.",
                nameof(token));
        }

        if (token.Length > 256)
        {
            throw new ArgumentException(
                "El refresh token excede el tamaño permitido.",
                nameof(token));
        }

        var tokenBytes = Encoding.UTF8.GetBytes(token);

        try
        {
            var hashBytes = SHA256.HashData(tokenBytes);

            return Convert.ToHexString(hashBytes)
                .ToLowerInvariant();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(tokenBytes);
        }
    }
}
