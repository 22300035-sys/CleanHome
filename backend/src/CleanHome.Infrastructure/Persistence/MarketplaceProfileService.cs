using CleanHome.Application.Abstractions.Marketplace;
using CleanHome.Application.Features.Marketplace;
using CleanHome.Domain.Entities.Marketplace;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Infrastructure.Persistence;

public sealed class MarketplaceProfileService
    : IMarketplaceProfileService
{
    private readonly CleanHomeDbContext _db;

    public MarketplaceProfileService(CleanHomeDbContext db)
    {
        _db = db;
    }

    public async Task<UserProfileResponse?> GetUserProfileAsync(
        Guid userId,
        CancellationToken ct)
    {
        var profile = await _db.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.UserId == userId, ct);

        return profile is null ? null : new(
            profile.UserId,
            profile.City,
            profile.Phone);
    }

    public async Task<UserProfileResponse> UpdateUserProfileAsync(
        Guid userId,
        UpdateUserProfileRequest request,
        CancellationToken ct)
    {
        var profile = await _db.UserProfiles
            .SingleOrDefaultAsync(x => x.UserId == userId, ct);

        if (profile is null)
        {
            profile = new UserProfile(
                userId, request.City, request.Phone);

            _db.UserProfiles.Add(profile);
        }
        else
        {
            profile.Update(request.City, request.Phone);
        }

        await _db.SaveChangesAsync(ct);

        return new UserProfileResponse(
            profile.UserId, profile.City, profile.Phone);
    }

    public async Task<WorkerProfileResponse> UpsertWorkerProfileAsync(
        Guid userId,
        UpsertWorkerProfileRequest request,
        CancellationToken ct)
    {
        var profile = await _db.WorkerProfiles
            .SingleOrDefaultAsync(x => x.UserId == userId, ct);

        if (profile is null)
        {
            profile = new WorkerProfile(
                userId,
                request.Description,
                request.ServiceCity,
                request.ServiceArea);

            _db.WorkerProfiles.Add(profile);
        }
        else
        {
            profile.Update(
                request.Description,
                request.ServiceCity,
                request.ServiceArea);
        }

        await _db.SaveChangesAsync(ct);
        return MapProfile(profile);
    }

    public async Task<WorkerDashboardResponse?> GetWorkerDashboardAsync(
        Guid userId,
        CancellationToken ct)
    {
        var profile = await _db.WorkerProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId, ct);

        if (profile is null)
            return null;

        var offerings = await GetOfferingsAsync(profile.Id, ct);

        var slots = await _db.WorkerAvailabilities
            .AsNoTracking()
            .Where(x => x.WorkerProfileId == profile.Id)
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.StartTime)
            .Select(x => new AvailabilitySlotResponse(
                x.Id, x.DayOfWeek, x.StartTime, x.EndTime))
            .ToListAsync(ct);

        return new WorkerDashboardResponse(
            MapProfile(profile),
            offerings,
            slots);
    }

    public async Task<WorkerOfferingResponse> UpsertOfferingAsync(
        Guid userId,
        UpsertOfferingRequest request,
        CancellationToken ct)
    {
        var profile = await RequireWorkerAsync(userId, ct);

        var category = await _db.ServiceCategories
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == request.ServiceCategoryId && x.IsActive,
                ct);

        if (category is null)
            throw new ArgumentException(
                "La categoría seleccionada no existe o está inactiva.");

        var offering = await _db.WorkerOfferings
            .SingleOrDefaultAsync(
                x => x.WorkerProfileId == profile.Id &&
                     x.ServiceCategoryId == request.ServiceCategoryId,
                ct);

        if (offering is null)
        {
            offering = new WorkerOffering(
                profile.Id,
                category.Id,
                request.ReferencePrice,
                request.Description);

            _db.WorkerOfferings.Add(offering);
        }
        else
        {
            offering.Update(
                request.ReferencePrice,
                request.Description);
        }

        await _db.SaveChangesAsync(ct);

        return new WorkerOfferingResponse(
            offering.Id,
            category.Id,
            category.Name,
            offering.ReferencePrice,
            offering.Description,
            offering.IsActive);
    }

    public async Task SetOfferingActiveAsync(
        Guid userId,
        Guid offeringId,
        bool active,
        CancellationToken ct)
    {
        var profile = await RequireWorkerAsync(userId, ct);

        var offering = await _db.WorkerOfferings
            .SingleOrDefaultAsync(
                x => x.Id == offeringId &&
                     x.WorkerProfileId == profile.Id,
                ct);

        if (offering is null)
            throw new KeyNotFoundException("Servicio no encontrado.");

        offering.SetActive(active);

        await _db.SaveChangesAsync(ct);
    }

    public async Task ReplaceAvailabilityAsync(
        Guid userId,
        IReadOnlyList<AvailabilitySlotRequest> slots,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(slots);

        if (slots.Count > 21)
            throw new ArgumentException(
                "Se permiten como máximo 21 intervalos.");

        foreach (var slot in slots)
        {
            if (slot.DayOfWeek is < 0 or > 6 ||
                slot.StartTime >= slot.EndTime)
            {
                throw new ArgumentException(
                    "La disponibilidad contiene intervalos inválidos.");
            }
        }

        foreach (var group in slots.GroupBy(x => x.DayOfWeek))
        {
            var ordered = group.OrderBy(x => x.StartTime).ToArray();

            for (var i = 1; i < ordered.Length; i++)
            {
                if (ordered[i].StartTime < ordered[i - 1].EndTime)
                {
                    throw new ArgumentException(
                        "Los horarios del mismo día no deben superponerse.");
                }
            }
        }

        var profile = await RequireWorkerAsync(userId, ct);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(ct);

        var existing = await _db.WorkerAvailabilities
            .Where(x => x.WorkerProfileId == profile.Id)
            .ToListAsync(ct);

        _db.WorkerAvailabilities.RemoveRange(existing);

        foreach (var slot in slots)
        {
            _db.WorkerAvailabilities.Add(
                new WorkerAvailability(
                    profile.Id,
                    slot.DayOfWeek,
                    slot.StartTime,
                    slot.EndTime));
        }

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task SetPublishedAsync(
        Guid userId,
        bool published,
        CancellationToken ct)
    {
        var profile = await RequireWorkerAsync(userId, ct);

        if (published)
        {
            var hasActiveOffering = await _db.WorkerOfferings
                .AnyAsync(
                    x => x.WorkerProfileId == profile.Id &&
                         x.IsActive,
                    ct);

            if (!hasActiveOffering)
                throw new InvalidOperationException(
                    "Debes configurar un servicio activo.");
        }

        profile.SetPublished(published);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<WorkerListingResponse>> BrowseWorkersAsync(
        string? city,
        Guid? categoryId,
        CancellationToken ct)
    {
        city = city?.Trim();

        if (city?.Length > 120)
            throw new ArgumentException("Ciudad inválida.");

        var query =
            from worker in _db.WorkerProfiles.AsNoTracking()
            join user in _db.Users.AsNoTracking()
                on worker.UserId equals user.Id
            where worker.IsPublished &&
                  worker.VerificationStatus == "approved" &&
                  user.IsActive &&
                  user.IsEmailVerified
            select new { worker, user };

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(x =>
                x.worker.ServiceCity.ToLower() == city.ToLower());
        }

        if (categoryId.HasValue)
        {
            var category = categoryId.Value;

            query = query.Where(x =>
                _db.WorkerOfferings.Any(o =>
                    o.WorkerProfileId == x.worker.Id &&
                    o.ServiceCategoryId == category &&
                    o.IsActive));
        }

        var workers = await query
            .OrderBy(x => x.worker.ServiceCity)
            .ThenBy(x => x.user.FirstName)
            .Take(50)
            .ToListAsync(ct);

        var results = new List<WorkerListingResponse>();

        foreach (var item in workers)
        {
            var offerings = await GetOfferingsAsync(
                item.worker.Id, ct, activeOnly: true);

            results.Add(new WorkerListingResponse(
                item.worker.Id,
                $"{item.user.FirstName} {item.user.LastName}",
                item.worker.Description,
                item.worker.ServiceCity,
                item.worker.ServiceArea,
                offerings));
        }

        return results;
    }

    private async Task<WorkerProfile> RequireWorkerAsync(
        Guid userId,
        CancellationToken ct)
    {
        return await _db.WorkerProfiles
            .SingleOrDefaultAsync(x => x.UserId == userId, ct)
            ?? throw new KeyNotFoundException(
                "Primero debes crear tu perfil profesional.");
    }

    private async Task<List<WorkerOfferingResponse>> GetOfferingsAsync(
        Guid workerProfileId,
        CancellationToken ct,
        bool activeOnly = false)
    {
        var query =
            from offering in _db.WorkerOfferings.AsNoTracking()
            join category in _db.ServiceCategories.AsNoTracking()
                on offering.ServiceCategoryId equals category.Id
            where offering.WorkerProfileId == workerProfileId
            select new { offering, category };

        if (activeOnly)
        {
            query = query.Where(x =>
                x.offering.IsActive &&
                x.category.IsActive);
        }

        return await query
            .OrderBy(x => x.category.Name)
            .Select(x => new WorkerOfferingResponse(
                x.offering.Id,
                x.category.Id,
                x.category.Name,
                x.offering.ReferencePrice,
                x.offering.Description,
                x.offering.IsActive))
            .ToListAsync(ct);
    }

    private static WorkerProfileResponse MapProfile(
        WorkerProfile profile)
    {
        return new WorkerProfileResponse(
            profile.Id,
            profile.UserId,
            profile.Description,
            profile.ServiceCity,
            profile.ServiceArea,
            profile.VerificationStatus,
            profile.IsPublished);
    }
}
