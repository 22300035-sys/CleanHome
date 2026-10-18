using CleanHome.Application.Abstractions.Catalog;
using CleanHome.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Infrastructure.Persistence.Repositories;

public sealed class ServiceCategoryRepository
    : IServiceCategoryRepository
{
    private readonly CleanHomeDbContext _context;

    public ServiceCategoryRepository(
        CleanHomeDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ServiceCategory>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.ServiceCategories
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceCategory?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.ServiceCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }
}
