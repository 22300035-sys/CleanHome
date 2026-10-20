namespace CleanHome.Application.Abstractions.Identity;

public interface IRefreshTokenGenerator
{
    GeneratedRefreshToken Generate();

    string ComputeHash(string token);
}

public sealed record GeneratedRefreshToken(
    string Token,
    string TokenHash);
