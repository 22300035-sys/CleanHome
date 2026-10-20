using CleanHome.Application.Features.Identity;

namespace CleanHome.Application.Abstractions.Identity;

public interface IAuthenticationService
{
    Task<AuthenticationResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthenticationResponse> RefreshAsync(
        RefreshSessionRequest request,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task LogoutAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SessionResponse>> GetSessionsAsync(
        Guid userId,
        Guid currentSessionId,
        CancellationToken cancellationToken = default);

    Task<CurrentUserResponse?> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
