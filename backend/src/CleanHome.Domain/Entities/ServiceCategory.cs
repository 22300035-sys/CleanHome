namespace CleanHome.Domain.Entities;

public sealed class ServiceCategory
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    private ServiceCategory()
    {
    }

    public ServiceCategory(
        string name,
        string slug,
        string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre es obligatorio.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("El slug es obligatorio.");

        name = name.Trim();
        slug = slug.Trim().ToLowerInvariant();
        description = description?.Trim() ?? string.Empty;

        if (name.Length > 120 ||
            slug.Length > 120 ||
            description.Length > 1000)
        {
            throw new ArgumentException(
                "Se excedió la longitud permitida.");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(
            slug,
            "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
        {
            throw new ArgumentException(
                "El slug solo admite letras minúsculas, números y guiones.");
        }

        Id = Guid.NewGuid();
        Name = name;
        Slug = slug;
        Description = description;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
