using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Authorization;
using Lms.Application.Departments.Dtos;
using Lms.Domain.Departments;
using Mediator;

namespace Lms.Application.Departments.Commands.CreateDepartment;

public sealed class CreateDepartmentHandler(
    IDepartmentRepository repository,
    IDepartmentBranchGuard branchGuard,
    IIdGenerator idGenerator,
    ITenantContext tenant)
    : IRequestHandler<CreateDepartmentCommand, Result<DepartmentDto>>
{
    public async ValueTask<Result<DepartmentDto>> Handle(CreateDepartmentCommand command, CancellationToken cancellationToken)
    {
        if (tenant.OrganizationId == Guid.Empty)
            return Result.Unauthorized();

        // ABAC: creating under a parent requires that parent to be in the caller's branch; creating a
        // root requires full-scope (a root sits outside any branch). Guard runs before existence checks
        // so a branch-limited caller cannot probe for departments outside their branch.
        if (command.ParentId is { } parentId)
        {
            await branchGuard.EnsureCanManageAsync(parentId, cancellationToken);

            if (await repository.FindByIdAsync(parentId, cancellationToken) is null)
                return Result.NotFound("Parent department not found.");
        }
        else
        {
            await branchGuard.EnsureFullScopeAsync(cancellationToken);
        }

        var department = Department.Create(idGenerator.NewId(), tenant.OrganizationId, command.ParentId, command.Name);

        await repository.AddAsync(department, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Created(department.ToDto());
    }
}
