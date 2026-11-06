using System.Data;
using CleanHome.Application.Marketplace;
using CleanHome.Domain.Entities;
using CleanHome.Infrastructure.Marketplace;
using CleanHome.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Infrastructure.Payments;

public sealed class PaymentService(CleanHomeDbContext db, MarketplaceService marketplace, IEnumerable<IPaymentGateway> gateways)
{
    private IPaymentGateway Gateway(string provider) => gateways.SingleOrDefault(x => x.Name == provider) ?? throw new BusinessException(400, "Proveedor inválido: stripe o mercadopago.");

    public async Task<Payment> CheckoutAsync(UserProfile actor, Guid bookingId, string provider, CancellationToken ct)
    {
        var gateway = Gateway(provider);
        gateway.ValidateConfiguration();
        Payment payment;
        await using (var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct))
        {
            var booking = await marketplace.GetBookingAsync(actor, bookingId, ct);
            if (booking.CustomerId != actor.Id) throw new BusinessException(403, "Solo el cliente puede pagar.");
            if (booking.Status != BookingStatus.Confirmed) throw new BusinessException(409, "La reserva no admite pagos.");
            var existing = await db.Payments.SingleOrDefaultAsync(x => x.BookingId == bookingId, ct);
            if (existing is not null)
            {
                if (existing.Provider != provider || existing.Status != PaymentStatus.Pending) throw new BusinessException(409, "La reserva ya tiene un pago o proveedor asignado.");
                if (existing.CheckoutUrl is null) throw new BusinessException(409, "Checkout en creación o requiere conciliación administrativa; no se emitirá un segundo cobro.");
                return existing;
            }
            payment = new Payment { BookingId = bookingId, Provider = provider, Amount = booking.Amount, Currency = booking.Currency };
            db.Payments.Add(payment);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        var checkout = await gateway.CreateCheckoutAsync(payment, ct);
        // Un webhook puede llegar durante la llamada externa. Actualizar solo URL/ID sin sobrescribir su estado.
        await db.Entry(payment).ReloadAsync(ct);
        payment.CheckoutId = checkout.Id;
        payment.CheckoutUrl = checkout.Url;
        payment.Version = Guid.NewGuid();
        await db.SaveChangesAsync(ct);
        return payment;
    }

    public async Task WebhookAsync(string provider, string body, IReadOnlyDictionary<string, string> headers, IReadOnlyDictionary<string, string> query, CancellationToken ct)
    {
        var verified = await Gateway(provider).VerifyWebhookAsync(body, headers, query, ct);
        if (verified is null) return;
        if (!Guid.TryParse(verified.PaymentReference, out var id)) throw new BusinessException(400, "Referencia inválida.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var payment = await db.Payments.SingleOrDefaultAsync(x => x.Id == id && x.Provider == provider, ct) ?? throw new BusinessException(404, "Pago desconocido.");
        if (payment.Amount != verified.Amount || payment.Currency != verified.Currency) throw new BusinessException(409, "El importe o moneda del proveedor no coincide.");
        if (payment.ProviderPaymentId is not null && payment.ProviderPaymentId != verified.ProviderPaymentId) throw new BusinessException(409, "Cobro duplicado: se requiere conciliación.");
        if (payment.Status == verified.Status || (verified.Status == PaymentStatus.Paid && payment.Status is PaymentStatus.Refunded or PaymentStatus.Disputed)) return;
        payment.ProviderPaymentId = verified.ProviderPaymentId;
        payment.Status = verified.Status;
        var booking = await db.Bookings.SingleAsync(x => x.Id == payment.BookingId, ct);
        db.Notifications.AddRange(new Notification { RecipientId = booking.CustomerId, Kind = "payment." + verified.Status, ResourceId = id }, new Notification { RecipientId = booking.WorkerId, Kind = "payment." + verified.Status, ResourceId = id });
        payment.Version = Guid.NewGuid();
        db.Audit.Add(new AuditEntry { ActorUserId = provider, Action = "payment." + verified.Status, ResourceId = id });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task RefundAsync(string adminUserId, Guid id, CancellationToken ct)
    {
        var payment = await db.Payments.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new BusinessException(404, "Pago desconocido.");
        if (payment.Status == PaymentStatus.Refunded) return;
        if (payment.Status != PaymentStatus.Paid) throw new BusinessException(409, "Solo se reembolsan pagos confirmados.");
        await Gateway(payment.Provider).RefundAsync(payment, ct);
        await db.Entry(payment).ReloadAsync(ct);
        payment.Status = PaymentStatus.Refunded;
        payment.Version = Guid.NewGuid();
        db.Audit.Add(new AuditEntry { ActorUserId = adminUserId, Action = "payment.refund", ResourceId = id });
        await db.SaveChangesAsync(ct);
    }
}
