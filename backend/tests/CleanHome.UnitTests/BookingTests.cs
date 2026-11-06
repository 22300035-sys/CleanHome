using CleanHome.Domain.Entities;
using CleanHome.Infrastructure.Payments;

namespace CleanHome.UnitTests;

public class BookingTests
{
    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, true)]
    [InlineData(false, false, true)]
    public void OnlyPaidBookingCanBeStartedByItsWorker(bool customer, bool worker, bool paid)
    {
        var booking = new ServiceBooking();
        Assert.Throws<InvalidOperationException>(() => booking.Transition(BookingStatus.InProgress, customer, worker, paid));
    }
    [Fact]
    public void PaidServiceRequiresWorkerStartAndCustomerCompletion()
    {
        var booking = new ServiceBooking();
        booking.Transition(BookingStatus.InProgress, false, true, true);
        Assert.Throws<InvalidOperationException>(() => booking.Transition(BookingStatus.Completed, false, true, true));
        booking.Transition(BookingStatus.Completed, true, false, true);
        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.Throws<InvalidOperationException>(() => booking.Transition(BookingStatus.InProgress, false, true, true));
    }
    [Fact]
    public void PaidBookingCannotBeCancelledBeforeRefund()
    {
        var booking = new ServiceBooking();
        Assert.Throws<InvalidOperationException>(() => booking.Transition(BookingStatus.Cancelled, true, false, true));
        booking.Transition(BookingStatus.Cancelled, true, false, false);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }
    [Fact]
    public void RequestCannotBeAcceptedTwiceOrAfterItsStart()
    {
        var request = new ServiceRequest { StartsAt = DateTimeOffset.UtcNow.AddDays(1) };
        request.Book();
        Assert.Throws<InvalidOperationException>(request.Book);
        Assert.Throws<InvalidOperationException>(() => new ServiceRequest { StartsAt = DateTimeOffset.UtcNow.AddDays(-1) }.Book());
    }
    [Fact]
    public void WebhookSignaturesRejectWrongKeyTamperingAndMalformedHex()
    {
        const string signature = "b0344c61d8db38535ca8afceaf0bf12b881dc200c9833da726e9376c2e32cff7";
        // RFC 4231 test vector: 20 bytes 0x0b, "Hi There".
        var secret = new string((char)0x0b, 20);
        Assert.True(WebhookSignatures.Verify(secret, "Hi There", [signature]));
        Assert.False(WebhookSignatures.Verify(secret, "tampered", [signature]));
        Assert.False(WebhookSignatures.Verify("wrong", "Hi There", [signature]));
        Assert.False(WebhookSignatures.Verify(secret, "Hi There", ["not-hex"]));
        Assert.False(WebhookSignatures.Verify("", "Hi There", [signature]));
    }
    [Fact]
    public void WebhookTimestampRejectsReplayAndFutureDates()
    {
        Assert.True(WebhookSignatures.IsRecent(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));
        Assert.False(WebhookSignatures.IsRecent(DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds().ToString()));
        Assert.False(WebhookSignatures.IsRecent(DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString()));
        Assert.False(WebhookSignatures.IsRecent("9223372036854775807"));
    }
}
