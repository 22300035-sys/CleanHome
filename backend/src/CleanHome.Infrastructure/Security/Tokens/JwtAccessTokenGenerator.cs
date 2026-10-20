using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CleanHome.Application.Abstractions.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CleanHome.Infrastructure.Security.Tokens;

public sealed class JwtAccessTokenGenerator
    : IAccessTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _credentials;

    public JwtAccessTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.Issuer))
        {
            throw new InvalidOperationException(
                "Falta configurar Jwt:Issuer.");
        }

        if (string.IsNullOrWhiteSpace(_options.Audience))
        {
            throw new InvalidOperationException(
                "Falta configurar Jwt:Audience.");
        }

        if (_options.AccessTokenMinutes is < 1 or > 15)
        {
            throw new InvalidOperationException(
                "Jwt:AccessTokenMinutes debe estar entre 1 y 15.");
        }

        if (string.IsNullOrWhiteSpace(_options.SigningKey))
        {
            throw new InvalidOperationException(
                "Falta configurar Jwt:SigningKey.");
        }

        var keyBytes = Encoding.UTF8.GetBytes(
            _options.SigningKey);

        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "La clave JWT debe tener al menos 32 bytes.");
        }

        var securityKey = new SymmetricSecurityKey(keyBytes);

        _credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);
    }

    public GeneratedAccessToken Generate(
        Guid userId,
        Guid sessionId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "El usuario es obligatorio.",
                nameof(userId));
        }

        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException(
                "La sesión es obligatoria.",
                nameof(sessionId));
        }

        var now = DateTimeOffset.UtcNow;

        var expiresAt = now.AddMinutes(
            _options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                userId.ToString()),

            new(
                "sid",
                sessionId.ToString()),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString("N"))
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: _credentials);

        var tokenString =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return new GeneratedAccessToken(
            tokenString,
            expiresAt);
    }
}
