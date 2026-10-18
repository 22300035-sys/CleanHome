using CleanHome.Domain.Entities;

namespace CleanHome.Application.Abstractions.Catalog;

public interface IServiceCategoryRepository
{
    Task<IReadOnlyList<ServiceCategory>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task<ServiceCategory?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
