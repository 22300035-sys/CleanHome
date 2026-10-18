using CleanHome.Application.Abstractions.Catalog;

namespace CleanHome.Application.Features.Catalog;

public sealed class ServiceCategoryService
{
    private readonly IServiceCategoryRepository _repository;

    public ServiceCategoryService(
        IServiceCategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ServiceCategoryDto>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await _repository.GetActiveAsync(
            cancellationToken);

        return categories
            .Select(x => new ServiceCategoryDto(
                x.Id,
                x.Name,
                x.Slug,
                x.Description))
            .ToList();
    }

    public async Task<ServiceCategoryDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (category is null || !category.IsActive)
            return null;

        return new ServiceCategoryDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description);
    }
}
