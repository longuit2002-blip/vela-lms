using Ardalis.Result;
using Lms.Application.Organizations.Dtos;
using Mediator;

namespace Lms.Application.Organizations.Commands.CreateOrganization;

/// <summary>
/// Creates a new organization (tenant).
/// <para>
/// Authorization audit (U8): intentionally <b>not</b> gated by an org-scoped <c>IRequirePermission</c>.
/// It is not exposed by any API endpoint this slice — organizations are provisioned via the seed/platform
/// path, which bypasses the Mediator pipeline. Creating a tenant is a cross-tenant <em>platform</em>
/// operation; when the platform provisioning surface is built (the A4 seam), it must be gated by a
/// platform role, not by a permission resolved within a single organization's context.
/// </para>
/// </summary>
public sealed record CreateOrganizationCommand(string Name, string Slug) : IRequest<Result<OrganizationDto>>;
