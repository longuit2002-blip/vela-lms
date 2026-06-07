using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Organizations.Dtos;
using Lms.Domain.Organizations;
using Mediator;

namespace Lms.Application.Organizations.Commands.CreateOrganization;

public sealed class CreateOrganizationHandler(IOrganizationRepository repository, IIdGenerator idGenerator)
    : IRequestHandler<CreateOrganizationCommand, Result<OrganizationDto>>
{
    public async ValueTask<Result<OrganizationDto>> Handle(
        CreateOrganizationCommand command,
        CancellationToken cancellationToken)
    {
        // Domain enforces invariants and normalizes the slug.
        var organization = Organization.Create(idGenerator.NewId(), command.Name, command.Slug);

        if (await repository.SlugExistsAsync(organization.Slug, cancellationToken))
            return Result.Conflict($"An organization with slug '{organization.Slug}' already exists.");

        await repository.AddAsync(organization, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Created(organization.ToDto());
    }
}
