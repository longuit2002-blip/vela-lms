using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Authorization;
using Mediator;

namespace Lms.Application.Departments.Commands.DeleteDepartment;

public sealed class DeleteDepartmentHandler(
    IDepartmentRepository repository,
    IDepartmentBranchGuard branchGuard)
    : IRequestHandler<DeleteDepartmentCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteDepartmentCommand command, CancellationToken cancellationToken)
    {
        var department = await repository.FindByIdAsync(command.DepartmentId, cancellationToken);
        if (department is null)
            return Result.NotFound();

        await branchGuard.EnsureCanManageAsync(department.Id, cancellationToken);

        // Block-if-non-empty: an admin must move/reassign children and users out first.
        if (await repository.HasChildrenAsync(department.Id, cancellationToken))
            return Result.Conflict("Cannot delete a department that has child departments.");
        if (await repository.HasAssignedUsersAsync(department.Id, cancellationToken))
            return Result.Conflict("Cannot delete a department that has assigned users.");

        await repository.RemoveAsync(department, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
