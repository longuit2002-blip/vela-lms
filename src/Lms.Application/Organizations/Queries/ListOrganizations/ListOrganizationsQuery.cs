using Ardalis.Result;
using Lms.Application.Organizations.Dtos;
using Mediator;

namespace Lms.Application.Organizations.Queries.ListOrganizations;

/// <summary>Lists all organizations (skeleton — pagination/scoping added in later phases).</summary>
public sealed record ListOrganizationsQuery : IRequest<Result<IReadOnlyList<OrganizationDto>>>;
