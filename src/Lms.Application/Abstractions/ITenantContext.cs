namespace Lms.Application.Abstractions;

/// <summary>
/// The current request's tenant, resolved from the JWT <c>org</c> claim only (never body/subdomain).
/// <see cref="OrganizationId"/> is <see cref="Guid.Empty"/> when no tenant is established (unauthenticated,
/// design-time, or pre-auth flows) — consumers treat that as "match nothing" (fail closed).
/// </summary>
public interface ITenantContext
{
    Guid OrganizationId { get; }

    bool HasTenant => OrganizationId != Guid.Empty;
}
