using CleanHome.Application.Abstractions.Identity;
using CleanHome.Application.Features.Identity;
using CleanHome.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CleanHome.Infrastructure.Persistence.Repositories.Identity;

public sealed class UserRegistrationRepository
    : IUserRegistrationRepository
{
    private readonly CleanHomeDbContext _dbContext;

    public UserRegistrationRepository(
        CleanHomeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> EmailExistsAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken);
    }

    public async Task AddAsync(
        User user,
        UserCredential credential,
        CancellationToken cancellationToken = default)
    {
        if (credential.UserId != user.Id)
        {
            throw new ArgumentException(
                "La credencial no corresponde al usuario.");
        }

        await _dbContext.Users.AddAsync(
            user,
            cancellationToken);

        await _dbContext.UserCredentials.AddAsync(
            credential,
            cancellationToken);

        try
        {
            // Una sola operación SaveChangesAsync garantiza
            // atomicidad para ambas inserciones con PostgreSQL.
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException exception)
            when (
                exception.InnerException is PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation,
                    ConstraintName: "IX_users_normalized_email"
                })
        {
            throw new DuplicateUserEmailException();
        }
    }
}
