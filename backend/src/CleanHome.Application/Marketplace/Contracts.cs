using System.ComponentModel.DataAnnotations;
using CleanHome.Domain.Entities;

namespace CleanHome.Application.Marketplace;

public sealed record ProfileInput([Required, StringLength(120, MinimumLength = 2)] string DisplayName, AccountRole Role);
public sealed record LocationInput([Required, StringLength(80)] string Label, [Required, StringLength(500)] string Address, [Required, StringLength(120)] string City);
public sealed record RequestInput(Guid LocationId, Guid CategoryId, [Required, StringLength(2000, MinimumLength = 10)] string Description, DateTimeOffset StartsAt, [Range(30, 720)] int DurationMinutes);
public sealed record QuoteInput([Range(typeof(decimal), "1", "100000")] decimal Amount, [StringLength(1000)] string Notes, DateTimeOffset ExpiresAt);
public sealed record MessageInput([Required, StringLength(2000, MinimumLength = 1)] string Text);
public sealed record ReviewInput(DocumentStatus Status, [StringLength(1000)] string Notes);
public sealed record BookingTransition(BookingStatus Status);
public sealed record CheckoutInput([Required] string Provider);
public sealed record CategoryInput([Required, StringLength(120)] string Name, [Required, StringLength(120)] string Slug, [StringLength(1000)] string Description);
public sealed record Page<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize);
public sealed record RequestView(Guid Id, Guid CategoryId, string Description, string City, DateTimeOffset StartsAt, int DurationMinutes, RequestStatus Status, DateTimeOffset CreatedAt);
public sealed record DocumentView(Guid Id, string OriginalName, string ContentType, long Length, DocumentStatus Status, string ReviewNotes, DateTimeOffset CreatedAt);
public sealed record CheckoutResult(string Id, string Url);
public sealed record VerifiedPayment(string PaymentReference, string ProviderPaymentId, decimal Amount, string Currency, PaymentStatus Status);

public sealed class BusinessException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public interface IPaymentGateway
{
    string Name { get; }
    void ValidateConfiguration();
    Task<CheckoutResult> CreateCheckoutAsync(Payment payment, CancellationToken ct);
    Task<VerifiedPayment?> VerifyWebhookAsync(string body, IReadOnlyDictionary<string, string> headers, IReadOnlyDictionary<string, string> query, CancellationToken ct);
    Task RefundAsync(Payment payment, CancellationToken ct);
}

public interface IPrivateFileStore
{
    Task<string> WriteAsync(Stream source, CancellationToken ct);
    Stream Read(string key);
    void Delete(string key);
}
