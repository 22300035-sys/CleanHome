namespace CleanHome.Domain.Entities;

public enum AccountRole { Customer, Worker }
public enum RequestStatus { Open, Booked, Cancelled, Completed }
public enum BookingStatus { Confirmed, InProgress, Completed, Cancelled }
public enum DocumentStatus { Pending, Approved, Rejected }
public enum PaymentStatus { Pending, Paid, Refunded, Disputed }

public sealed class UserProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public AccountRole Role { get; set; }
    public bool IsVerified { get; set; }
    public bool IsSuspended { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CustomerLocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public string Label { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
}

public sealed class ServiceRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Guid LocationId { get; set; }
    public Guid CategoryId { get; set; }
    public string Description { get; set; } = "";
    public DateTimeOffset StartsAt { get; set; }
    public int DurationMinutes { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Open;
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public void Book()
    {
        if (Status != RequestStatus.Open || StartsAt <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException("La solicitud ya no admite reservas.");
        Status = RequestStatus.Booked;
        Version = Guid.NewGuid();
    }
}

public sealed class ServiceQuote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public Guid WorkerId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "MXN";
    public string Notes { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ServiceBooking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public Guid QuoteId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid WorkerId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "MXN";
    public BookingStatus Status { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public void Transition(BookingStatus next, bool isCustomer, bool isWorker, bool paid)
    {
        var allowed = next switch
        {
            BookingStatus.InProgress => isWorker && paid && Status == BookingStatus.Confirmed,
            BookingStatus.Completed => isCustomer && Status == BookingStatus.InProgress,
            BookingStatus.Cancelled => (isCustomer || isWorker) && !paid && Status == BookingStatus.Confirmed,
            _ => false
        };
        if (!allowed) throw new InvalidOperationException("Transición no permitida; para cancelar un pago se requiere reembolso administrativo.");
        Status = next;
        Version = Guid.NewGuid();
    }
}

public sealed class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Guid SenderId { get; set; }
    public string Text { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PrivateDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public string StorageKey { get; set; } = "";
    public string OriginalName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Length { get; set; }
    public DocumentStatus Status { get; set; }
    public string ReviewNotes { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ActorUserId { get; set; } = "";
    public string Action { get; set; } = "";
    public Guid ResourceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipientId { get; set; }
    public string Kind { get; set; } = "";
    public Guid ResourceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }
}

public sealed class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public string Provider { get; set; } = "";
    public string? CheckoutId { get; set; }
    public string? ProviderPaymentId { get; set; }
    public string? CheckoutUrl { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "MXN";
    public PaymentStatus Status { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
