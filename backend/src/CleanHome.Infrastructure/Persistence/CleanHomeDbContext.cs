using CleanHome.Domain.Entities;
using CleanHome.Domain.Entities.Bookings;
using CleanHome.Domain.Entities.Community;
using CleanHome.Domain.Entities.Identity;
using CleanHome.Domain.Entities.Marketplace;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Infrastructure.Persistence;

public sealed class CleanHomeDbContext : DbContext
{
    public CleanHomeDbContext(
        DbContextOptions<CleanHomeDbContext> options)
        : base(options)
    {
    }

    // Catálogo
    public DbSet<ServiceCategory> ServiceCategories =>
        Set<ServiceCategory>();

    // Identidad
    public DbSet<User> Users =>
        Set<User>();

    public DbSet<UserCredential> UserCredentials =>
        Set<UserCredential>();

    public DbSet<UserSession> UserSessions =>
        Set<UserSession>();

    public DbSet<RefreshToken> RefreshTokens =>
        Set<RefreshToken>();

    public DbSet<SecurityEvent> SecurityEvents =>
        Set<SecurityEvent>();

    public DbSet<EmailVerificationToken> EmailVerificationTokens =>
        Set<EmailVerificationToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens =>
        Set<PasswordResetToken>();

    // Perfiles y servicios
    public DbSet<UserProfile> UserProfiles =>
        Set<UserProfile>();

    public DbSet<WorkerProfile> WorkerProfiles =>
        Set<WorkerProfile>();

    public DbSet<WorkerOffering> WorkerOfferings =>
        Set<WorkerOffering>();

    public DbSet<WorkerAvailability> WorkerAvailabilities =>
        Set<WorkerAvailability>();

    public DbSet<WorkerDocument> WorkerDocuments =>
        Set<WorkerDocument>();

    // Solicitudes y reservas
    public DbSet<ServiceRequest> ServiceRequests =>
        Set<ServiceRequest>();

    public DbSet<ServiceRequestAddress> ServiceRequestAddresses =>
        Set<ServiceRequestAddress>();

    public DbSet<ServiceQuote> ServiceQuotes =>
        Set<ServiceQuote>();

    public DbSet<ServiceBooking> ServiceBookings =>
        Set<ServiceBooking>();

    public DbSet<BookingStatusHistory> BookingStatusHistories =>
        Set<BookingStatusHistory>();

    // Comunicación y reputación
    public DbSet<ChatMessage> ChatMessages =>
        Set<ChatMessage>();

    public DbSet<UserNotification> UserNotifications =>
        Set<UserNotification>();

    public DbSet<ServiceReview> ServiceReviews =>
        Set<ServiceReview>();

    // Soporte
    public DbSet<SupportIncident> SupportIncidents =>
        Set<SupportIncident>();

    public DbSet<IncidentComment> IncidentComments =>
        Set<IncidentComment>();

    // Administración
    public DbSet<AdminMembership> AdminMemberships =>
        Set<AdminMembership>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CleanHomeDbContext).Assembly);
    }
}
