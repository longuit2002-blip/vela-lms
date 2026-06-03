using Ardalis.Result;
using Lms.Application.Organizations.Dtos;
using Mediator;

namespace Lms.Application.Organizations.Queries.GetMyOrganization;

/// <summary>
/// Returns the authenticated caller's own organization (resolved from the tenant context).
/// <para>
/// Authorization audit (U8): intentionally <b>not</b> gated by an <c>IRequirePermission</c> — this is a
/// self-scoped read of the caller's own organization (the org id comes from the JWT, never client input),
/// so any authenticated user may call it. The endpoint's <c>.RequireAuthorization()</c> is the only gate.
/// </para>
/// </summary>
public sealed record GetMyOrganizationQuery : IRequest<Result<OrganizationDto>>;
