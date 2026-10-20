namespace CleanHome.Domain.Entities.Marketplace;

public sealed class UserProfile
{
    public Guid UserId { get; private set; }
    public string City { get; private set; } = "";
    public string? Phone { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private UserProfile() { }

    public UserProfile(Guid userId, string city, string? phone)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("Usuario inválido.");

        UserId = userId;
        Update(city, phone);
    }

    public void Update(string city, string? phone)
    {
        city = city?.Trim() ?? "";
        phone = phone?.Trim();

        if (city.Length is < 2 or > 120)
            throw new ArgumentException("Ciudad inválida.");

        if (phone?.Length > 25)
            throw new ArgumentException("Teléfono demasiado largo.");

        City = city;
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class WorkerProfile
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    public string Description { get; private set; } = "";
    public string ServiceCity { get; private set; } = "";
    public string ServiceArea { get; private set; } = "";

    // pending, approved, rejected, suspended
    public string VerificationStatus { get; private set; } = "pending";

    public bool IsPublished { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private WorkerProfile() { }

    public WorkerProfile(
        Guid userId,
        string description,
        string serviceCity,
        string serviceArea)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("Usuario inválido.");

        Id = Guid.NewGuid();
        UserId = userId;
        VerificationStatus = "pending";
        CreatedAt = DateTimeOffset.UtcNow;

        Update(description, serviceCity, serviceArea);
    }

    public void Update(
        string description,
        string serviceCity,
        string serviceArea)
    {
        description = description?.Trim() ?? "";
        serviceCity = serviceCity?.Trim() ?? "";
        serviceArea = serviceArea?.Trim() ?? "";

        if (description.Length is < 20 or > 1500)
            throw new ArgumentException(
                "La descripción debe tener entre 20 y 1500 caracteres.");

        if (serviceCity.Length is < 2 or > 120)
            throw new ArgumentException("Ciudad de servicio inválida.");

        if (serviceArea.Length is < 2 or > 200)
            throw new ArgumentException("Zona de servicio inválida.");

        Description = description;
        ServiceCity = serviceCity;
        ServiceArea = serviceArea;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetPublished(bool published)
    {
        if (published && VerificationStatus != "approved")
            throw new InvalidOperationException(
                "El perfil requiere aprobación documental.");

        IsPublished = published;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Únicamente el módulo administrativo podrá invocarlo.
    public void SetVerificationStatus(string status)
    {
        if (status is not (
            "pending" or "approved" or "rejected" or "suspended"))
        {
            throw new ArgumentException("Estado inválido.");
        }

        VerificationStatus = status;

        if (status != "approved")
            IsPublished = false;

        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class WorkerOffering
{
    public Guid Id { get; private set; }
    public Guid WorkerProfileId { get; private set; }
    public Guid ServiceCategoryId { get; private set; }
    public decimal ReferencePrice { get; private set; }
    public string Description { get; private set; } = "";
    public bool IsActive { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private WorkerOffering() { }

    public WorkerOffering(
        Guid workerProfileId,
        Guid categoryId,
        decimal referencePrice,
        string description)
    {
        if (workerProfileId == Guid.Empty ||
            categoryId == Guid.Empty)
            throw new ArgumentException("Identificador inválido.");

        Id = Guid.NewGuid();
        WorkerProfileId = workerProfileId;
        ServiceCategoryId = categoryId;
        IsActive = true;

        Update(referencePrice, description);
    }

    public void Update(decimal referencePrice, string description)
    {
        description = description?.Trim() ?? "";

        if (referencePrice is < 1 or > 100000)
            throw new ArgumentException("Precio inválido.");

        if (description.Length > 500)
            throw new ArgumentException("Descripción demasiado larga.");

        ReferencePrice = referencePrice;
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetActive(bool active)
    {
        IsActive = active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class WorkerAvailability
{
    public Guid Id { get; private set; }
    public Guid WorkerProfileId { get; private set; }

    // DayOfWeek de .NET: domingo = 0, sábado = 6.
    public int DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }

    private WorkerAvailability() { }

    public WorkerAvailability(
        Guid workerProfileId,
        int dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        if (workerProfileId == Guid.Empty)
            throw new ArgumentException("Trabajador inválido.");

        if (dayOfWeek is < 0 or > 6)
            throw new ArgumentException("Día inválido.");

        if (startTime >= endTime)
            throw new ArgumentException(
                "La hora final debe ser posterior a la inicial.");

        Id = Guid.NewGuid();
        WorkerProfileId = workerProfileId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
    }
}

public sealed class WorkerDocument
{
    public Guid Id { get; private set; }
    public Guid WorkerProfileId { get; private set; }

    public string DocumentType { get; private set; } = "";
    public string StorageKey { get; private set; } = "";
    public string ReviewStatus { get; private set; } = "pending";
    public DateTimeOffset UploadedAt { get; private set; }

    private WorkerDocument() { }

    public WorkerDocument(
        Guid workerProfileId,
        string documentType,
        string storageKey)
    {
        if (workerProfileId == Guid.Empty)
            throw new ArgumentException("Trabajador inválido.");

        if (string.IsNullOrWhiteSpace(documentType) ||
            documentType.Length > 50 ||
            string.IsNullOrWhiteSpace(storageKey) ||
            storageKey.Length > 300)
        {
            throw new ArgumentException("Documento inválido.");
        }

        Id = Guid.NewGuid();
        WorkerProfileId = workerProfileId;
        DocumentType = documentType.Trim();
        StorageKey = storageKey.Trim();
        ReviewStatus = "pending";
        UploadedAt = DateTimeOffset.UtcNow;
    }

    public void SetReviewStatus(string status)
    {
        if (status is not ("pending" or "approved" or "rejected"))
            throw new ArgumentException("Estado inválido.");

        ReviewStatus = status;
    }
}
