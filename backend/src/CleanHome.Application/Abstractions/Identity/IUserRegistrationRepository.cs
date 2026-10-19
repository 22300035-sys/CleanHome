using CleanHome.Domain.Entities.Identity;

namespace CleanHome.Application.Abstractions.Identity;

public interface IUserRegistrationRepository
{
    Task<bool> EmailExistsAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        User user,
        UserCredential credential,
        CancellationToken cancellationToken = default);
}
