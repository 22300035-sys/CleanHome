namespace CleanHome.Application.Features.Catalog;

public sealed record ServiceCategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string Description
);
