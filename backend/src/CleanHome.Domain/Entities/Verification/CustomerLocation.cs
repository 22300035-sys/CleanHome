namespace CleanHome.Domain.Entities.Verification;

public sealed class CustomerLocation
{
    public Guid Id { get; private set; }
    public Guid CustomerUserId { get; private set; }

    public string Label { get; private set; } = "";
    public string Street { get; private set; } = "";
    public string ExteriorNumber { get; private set; } = "";
    public string? InteriorNumber { get; private set; }
    public string Neighborhood { get; private set; } = "";
    public string PostalCode { get; private set; } = "";
    public string City { get; private set; } = "";
    public string State { get; private set; } = "";
    public string? Reference { get; private set; }

    // pending, approved, rejected
    public string VerificationStatus { get; private set; } = "pending";

    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private CustomerLocation() { }

    public CustomerLocation(
        Guid customerUserId,
        string label,
        string street,
        string exteriorNumber,
        string? interiorNumber,
        string neighborhood,
        string postalCode,
        string city,
        string state,
        string? reference)
    {
        if (customerUserId == Guid.Empty)
            throw new ArgumentException("Cliente inválido.");

        Id = Guid.NewGuid();
        CustomerUserId = customerUserId;
        CreatedAt = DateTimeOffset.UtcNow;
        IsActive = true;

        Update(
            label, street, exteriorNumber, interiorNumber,
            neighborhood, postalCode, city, state, reference);
    }

    public void Update(
        string label,
        string street,
        string exteriorNumber,
        string? interiorNumber,
        string neighborhood,
        string postalCode,
        string city,
        string state,
        string? reference)
    {
        label = label?.Trim() ?? "";
        street = street?.Trim() ?? "";
        exteriorNumber = exteriorNumber?.Trim() ?? "";
        interiorNumber = interiorNumber?.Trim();
        neighborhood = neighborhood?.Trim() ?? "";
        postalCode = postalCode?.Trim() ?? "";
        city = city?.Trim() ?? "";
        state = state?.Trim() ?? "";
        reference = reference?.Trim();

        if (label.Length is < 2 or > 80 ||
            street.Length is < 2 or > 200 ||
            exteriorNumber.Length is < 1 or > 30 ||
            interiorNumber?.Length > 30 ||
            neighborhood.Length is < 2 or > 150 ||
            postalCode.Length != 5 ||
            !postalCode.All(c => c is >= '0' and <= '9') ||
            city.Length is < 2 or > 120 ||
            state.Length is < 2 or > 100 ||
            reference?.Length > 500)
        {
            throw new ArgumentException(
                "Los datos de la ubicación son inválidos.");
        }

        Label = label;
        Street = street;
        ExteriorNumber = exteriorNumber;
        InteriorNumber = string.IsNullOrWhiteSpace(interiorNumber)
            ? null : interiorNumber;
        Neighborhood = neighborhood;
        PostalCode = postalCode;
        City = city;
        State = state;
        Reference = string.IsNullOrWhiteSpace(reference)
            ? null : reference;

        // Cualquier cambio de dirección exige una nueva revisión.
        VerificationStatus = "pending";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetVerificationStatus(string status)
    {
        if (status is not ("pending" or "approved" or "rejected"))
            throw new ArgumentException("Estado inválido.");

        // El servicio administrativo comprobará el comprobante
        // aprobado antes de llamar este método.
        VerificationStatus = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
