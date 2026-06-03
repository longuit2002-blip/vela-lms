using Ardalis.Result;
using Lms.Application.Organizations.Dtos;
using Mediator;

namespace Lms.Application.Organizations.Queries.GetMyOrganization;

/// <summary>Returns the authenticated caller's own organization (resolved from the tenant context).</summary>
public sealed record GetMyOrganizationQuery : IRequest<Result<OrganizationDto>>;
