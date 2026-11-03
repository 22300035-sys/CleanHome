namespace CleanHome.Application.Features.Verification;

public sealed record CustomerLocationRequest(
    string Label,
    string Street,
    string ExteriorNumber,
    string? InteriorNumber,
    string Neighborhood,
    string PostalCode,
    string City,
    string State,
    string? Reference);

public sealed record CustomerLocationResponse(
    Guid Id,
    string Label,
    string Street,
    string ExteriorNumber,
    string? InteriorNumber,
    string Neighborhood,
    string PostalCode,
    string City,
    string State,
    string? Reference,
    string VerificationStatus,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
