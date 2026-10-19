namespace CleanHome.Application.Features.Identity;

public sealed record RegisterUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password);

public sealed record RegisterUserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsEmailVerified,
    DateTimeOffset CreatedAt);
