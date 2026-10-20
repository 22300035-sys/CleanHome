namespace CleanHome.Domain.Entities.Bookings;

public sealed class ServiceRequest
{
    public Guid Id { get; private set; }
    public Guid CustomerUserId { get; private set; }
    public Guid ServiceCategoryId { get; private set; }

    public string Title { get; private set; } = "";
    public string Description { get; private set; } = "";
    public string City { get; private set; } = "";

    public DateTimeOffset ScheduledStart { get; private set; }
    public int EstimatedDurationMinutes { get; private set; }

    // open, booked, cancelled, expired
    public string Status { get; private set; } = "open";

    public Guid? CopiedFromRequestId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private ServiceRequest() { }

    public ServiceRequest(
        Guid customerUserId,
        Guid serviceCategoryId,
        string title,
        string description,
        string city,
        DateTimeOffset scheduledStart,
        int estimatedDurationMinutes,
        Guid? copiedFromRequestId = null)
    {
        if (customerUserId == Guid.Empty ||
            serviceCategoryId == Guid.Empty)
            throw new ArgumentException("Identificador inválido.");

        if (copiedFromRequestId == Guid.Empty)
            throw new ArgumentException("Solicitud original inválida.");

        Id = Guid.NewGuid();
        CustomerUserId = customerUserId;
        ServiceCategoryId = serviceCategoryId;
        CopiedFromRequestId = copiedFromRequestId;

        UpdateDetails(
            title,
            description,
            city,
            scheduledStart,
            estimatedDurationMinutes);

        Status = "open";
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateDetails(
        string title,
        string description,
        string city,
        DateTimeOffset scheduledStart,
        int estimatedDurationMinutes)
    {
        if (Status != "open")
            throw new InvalidOperationException(
                "La solicitud ya no admite cambios.");

        title = title?.Trim() ?? "";
        description = description?.Trim() ?? "";
        city = city?.Trim() ?? "";

        if (title.Length is < 5 or > 150)
            throw new ArgumentException("Título inválido.");

        if (description.Length is < 20 or > 3000)
            throw new ArgumentException(
                "La descripción debe tener entre 20 y 3000 caracteres.");

        if (city.Length is < 2 or > 120)
            throw new ArgumentException("Ciudad inválida.");

        if (scheduledStart <= DateTimeOffset.UtcNow.AddMinutes(30))
            throw new ArgumentException(
                "La fecha debe estar al menos 30 minutos en el futuro.");

        if (estimatedDurationMinutes is < 60 or > 1440)
            throw new ArgumentException(
                "La duración debe estar entre 60 y 1440 minutos.");

        Title = title;
        Description = description;
        City = city;
        ScheduledStart = scheduledStart.ToUniversalTime();
        EstimatedDurationMinutes = estimatedDurationMinutes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkBooked()
    {
        if (Status != "open")
            throw new InvalidOperationException(
                "Esta solicitud no está disponible.");

        Status = "booked";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status != "open")
            throw new InvalidOperationException(
                "Solo puede cancelarse una solicitud abierta.");

        Status = "cancelled";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Expire()
    {
        if (Status != "open")
            throw new InvalidOperationException(
                "Solo puede expirar una solicitud abierta.");

        Status = "expired";
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class ServiceRequestAddress
{
    public Guid ServiceRequestId { get; private set; }

    public string Street { get; private set; } = "";
    public string ExteriorNumber { get; private set; } = "";
    public string? InteriorNumber { get; private set; }
    public string Neighborhood { get; private set; } = "";
    public string PostalCode { get; private set; } = "";
    public string State { get; private set; } = "";
    public string? Reference { get; private set; }

    private ServiceRequestAddress() { }

    public ServiceRequestAddress(
        Guid serviceRequestId,
        string street,
        string exteriorNumber,
        string? interiorNumber,
        string neighborhood,
        string postalCode,
        string state,
        string? reference)
    {
        if (serviceRequestId == Guid.Empty)
            throw new ArgumentException("Solicitud inválida.");

        ServiceRequestId = serviceRequestId;

        Update(
            street,
            exteriorNumber,
            interiorNumber,
            neighborhood,
            postalCode,
            state,
            reference);
    }

    public void Update(
        string street,
        string exteriorNumber,
        string? interiorNumber,
        string neighborhood,
        string postalCode,
        string state,
        string? reference)
    {
        street = street?.Trim() ?? "";
        exteriorNumber = exteriorNumber?.Trim() ?? "";
        interiorNumber = interiorNumber?.Trim();
        neighborhood = neighborhood?.Trim() ?? "";
        postalCode = postalCode?.Trim() ?? "";
        state = state?.Trim() ?? "";
        reference = reference?.Trim();

        if (street.Length is < 2 or > 200 ||
            exteriorNumber.Length is < 1 or > 30 ||
            neighborhood.Length is < 2 or > 150 ||
            state.Length is < 2 or > 100 ||
            postalCode.Length != 5 ||
            !postalCode.All(char.IsDigit) ||
            interiorNumber?.Length > 30 ||
            reference?.Length > 500)
        {
            throw new ArgumentException(
                "Los datos de la dirección son inválidos.");
        }

        Street = street;
        ExteriorNumber = exteriorNumber;
        InteriorNumber = string.IsNullOrWhiteSpace(interiorNumber)
            ? null : interiorNumber;
        Neighborhood = neighborhood;
        PostalCode = postalCode;
        State = state;
        Reference = string.IsNullOrWhiteSpace(reference)
            ? null : reference;
    }
}

public sealed class ServiceQuote
{
    public Guid Id { get; private set; }
    public Guid ServiceRequestId { get; private set; }
    public Guid WorkerProfileId { get; private set; }

    public decimal Amount { get; private set; }
    public string Description { get; private set; } = "";

    // pending, accepted, rejected, withdrawn
    public string Status { get; private set; } = "pending";

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    private ServiceQuote() { }

    public ServiceQuote(
        Guid serviceRequestId,
        Guid workerProfileId,
        decimal amount,
        string description,
        DateTimeOffset expiresAt)
    {
        if (serviceRequestId == Guid.Empty ||
            workerProfileId == Guid.Empty)
            throw new ArgumentException("Identificador inválido.");

        if (amount is < 1 or > 1000000)
            throw new ArgumentException("Monto inválido.");

        description = description?.Trim() ?? "";

        if (description.Length is < 10 or > 1500)
            throw new ArgumentException(
                "La propuesta debe tener entre 10 y 1500 caracteres.");

        if (expiresAt <= DateTimeOffset.UtcNow)
            throw new ArgumentException(
                "La cotización debe tener una expiración futura.");

        Id = Guid.NewGuid();
        ServiceRequestId = serviceRequestId;
        WorkerProfileId = workerProfileId;
        Amount = decimal.Round(amount, 2);
        Description = description;
        Status = "pending";
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt.ToUniversalTime();
    }

    public void Accept()
    {
        if (Status != "pending" ||
            ExpiresAt <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException(
                "La cotización no está disponible.");

        Status = "accepted";
    }

    public void Reject()
    {
        if (Status != "pending")
            return;

        Status = "rejected";
    }

    public void Withdraw()
    {
        if (Status != "pending")
            throw new InvalidOperationException(
                "La cotización no puede retirarse.");

        Status = "withdrawn";
    }
}

public sealed class ServiceBooking
{
    public Guid Id { get; private set; }
    public Guid ServiceRequestId { get; private set; }
    public Guid ServiceQuoteId { get; private set; }
    public Guid CustomerUserId { get; private set; }
    public Guid WorkerProfileId { get; private set; }

    public decimal AgreedAmount { get; private set; }
    public DateTimeOffset ScheduledStart { get; private set; }
    public int EstimatedDurationMinutes { get; private set; }

    // confirmed, in_progress, completed, cancelled
    public string Status { get; private set; } = "confirmed";

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private ServiceBooking() { }

    public ServiceBooking(
        ServiceRequest request,
        ServiceQuote quote)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(quote);

        if (request.Status != "booked" ||
            quote.Status != "accepted" ||
            quote.ServiceRequestId != request.Id)
        {
            throw new InvalidOperationException(
                "La solicitud y la cotización no están confirmadas.");
        }

        Id = Guid.NewGuid();
        ServiceRequestId = request.Id;
        ServiceQuoteId = quote.Id;
        CustomerUserId = request.CustomerUserId;
        WorkerProfileId = quote.WorkerProfileId;
        AgreedAmount = quote.Amount;
        ScheduledStart = request.ScheduledStart;
        EstimatedDurationMinutes = request.EstimatedDurationMinutes;

        Status = "confirmed";
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void Start()
    {
        if (Status != "confirmed")
            throw new InvalidOperationException(
                "La reserva no puede iniciarse.");

        Status = "in_progress";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        if (Status != "in_progress")
            throw new InvalidOperationException(
                "La reserva no puede finalizarse.");

        Status = "completed";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status is not ("confirmed" or "in_progress"))
            throw new InvalidOperationException(
                "La reserva no puede cancelarse.");

        Status = "cancelled";
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class BookingStatusHistory
{
    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string PreviousStatus { get; private set; } = "";
    public string NewStatus { get; private set; } = "";
    public string? Reason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private BookingStatusHistory() { }

    public BookingStatusHistory(
        Guid bookingId,
        Guid actorUserId,
        string previousStatus,
        string newStatus,
        string? reason = null)
    {
        if (bookingId == Guid.Empty ||
            actorUserId == Guid.Empty)
            throw new ArgumentException("Identificador inválido.");

        if (string.IsNullOrWhiteSpace(previousStatus) ||
            string.IsNullOrWhiteSpace(newStatus))
            throw new ArgumentException("Estado inválido.");

        reason = reason?.Trim();

        if (reason?.Length > 500)
            throw new ArgumentException("Motivo demasiado largo.");

        Id = Guid.NewGuid();
        BookingId = bookingId;
        ActorUserId = actorUserId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
