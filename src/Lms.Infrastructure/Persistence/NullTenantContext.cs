using Lms.Application.Abstractions;

namespace Lms.Infrastructure.Persistence;

/// <summary>
/// No-tenant context for design-time (migrations) and any host path without an HTTP request.
/// Reports <see cref="Guid.Empty"/> so tenant-filtered queries fail closed.
/// </summary>
public sealed class NullTenantContext : ITenantContext
{
    public static readonly NullTenantContext Instance = new();

    public Guid OrganizationId => Guid.Empty;
}
