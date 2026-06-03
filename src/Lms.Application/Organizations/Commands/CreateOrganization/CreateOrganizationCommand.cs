using Ardalis.Result;
using Lms.Application.Organizations.Dtos;
using Mediator;

namespace Lms.Application.Organizations.Commands.CreateOrganization;

/// <summary>Creates a new organization (tenant).</summary>
public sealed record CreateOrganizationCommand(string Name, string Slug) : IRequest<Result<OrganizationDto>>;
