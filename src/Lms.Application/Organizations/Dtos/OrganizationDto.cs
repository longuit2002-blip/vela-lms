using Lms.Domain.Organizations;

namespace Lms.Application.Organizations.Dtos;

/// <summary>Read model for an organization returned over the API.</summary>
public sealed record OrganizationDto(Guid Id, string Name, string Slug, string Status, DateTimeOffset CreatedAt);

/// <summary>Manual mapping (no AutoMapper) from the aggregate to its DTO.</summary>
public static class OrganizationMappings
{
    public static OrganizationDto ToDto(this Organization organization) =>
        new(
            organization.Id,
            organization.Name,
            organization.Slug,
            organization.Status.ToString(),
            organization.CreatedAt);
}
