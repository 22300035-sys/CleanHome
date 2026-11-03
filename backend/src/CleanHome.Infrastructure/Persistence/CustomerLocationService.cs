using CleanHome.Application.Abstractions.Verification;
using CleanHome.Application.Features.Verification;
using CleanHome.Domain.Entities.Verification;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Infrastructure.Persistence;

public sealed class CustomerLocationService : ICustomerLocationService
{
    private readonly CleanHomeDbContext _db;

    public CustomerLocationService(CleanHomeDbContext db)
    {
        _db = db;
    }

    public async Task<CustomerLocationResponse> CreateAsync(
        Guid userId,
        CustomerLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var location = new CustomerLocation(
            userId,
            request.Label,
            request.Street,
            request.ExteriorNumber,
            request.InteriorNumber,
            request.Neighborhood,
            request.PostalCode,
            request.City,
            request.State,
            request.Reference);

        _db.CustomerLocations.Add(location);
        await _db.SaveChangesAsync(cancellationToken);

        return ToResponse(location);
    }

    public async Task<IReadOnlyList<CustomerLocationResponse>> GetMyAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("Usuario invalido.");

        return await _db.CustomerLocations
            .AsNoTracking()
            .Where(x => x.CustomerUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new CustomerLocationResponse(
                x.Id,
                x.Label,
                x.Street,
                x.ExteriorNumber,
                x.InteriorNumber,
                x.Neighborhood,
                x.PostalCode,
                x.City,
                x.State,
                x.Reference,
                x.VerificationStatus,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    private static CustomerLocationResponse ToResponse(
        CustomerLocation location)
    {
        return new CustomerLocationResponse(
            location.Id,
            location.Label,
            location.Street,
            location.ExteriorNumber,
            location.InteriorNumber,
            location.Neighborhood,
            location.PostalCode,
            location.City,
            location.State,
            location.Reference,
            location.VerificationStatus,
            location.IsActive,
            location.CreatedAt,
            location.UpdatedAt);
    }
}
