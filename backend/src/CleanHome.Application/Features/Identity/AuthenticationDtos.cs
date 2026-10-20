namespace CleanHome.Application.Features.Identity;

public sealed record LoginRequest(
    string Email,
    string Password,
    string DeviceName,
    string Platform);

public sealed record RefreshSessionRequest(
    string RefreshToken);

public sealed record AuthenticationResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset SessionExpiresAt,
    Guid SessionId,
    string TokenType = "Bearer");

public sealed record SessionResponse(
    Guid Id,
    string DeviceName,
    string Platform,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActivityAt,
    DateTimeOffset ExpiresAt,
    bool IsCurrent);

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsEmailVerified);
