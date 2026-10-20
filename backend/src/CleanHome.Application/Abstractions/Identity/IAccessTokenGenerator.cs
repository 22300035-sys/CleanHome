namespace CleanHome.Application.Abstractions.Identity;

public interface IAccessTokenGenerator
{
    GeneratedAccessToken Generate(
        Guid userId,
        Guid sessionId);
}

public sealed record GeneratedAccessToken(
    string Token,
    DateTimeOffset ExpiresAt);
