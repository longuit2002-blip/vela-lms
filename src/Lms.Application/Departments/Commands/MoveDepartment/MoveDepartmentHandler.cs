using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Authorization;
using Lms.Application.Departments.Dtos;
using Mediator;

namespace Lms.Application.Departments.Commands.MoveDepartment;

public sealed class MoveDepartmentHandler(
    IDepartmentRepository repository,
    IDepartmentBranchGuard branchGuard,
    IDepartmentClosure closure)
    : IRequestHandler<MoveDepartmentCommand, Result<DepartmentDto>>
{
    public async ValueTask<Result<DepartmentDto>> Handle(MoveDepartmentCommand command, CancellationToken cancellationToken)
    {
        var department = await repository.FindByIdAsync(command.DepartmentId, cancellationToken);
        if (department is null)
            return Result.NotFound();

        // ABAC: a branch-limited caller must own the moved department...
        await branchGuard.EnsureCanManageAsync(department.Id, cancellationToken);

        if (command.NewParentId is { } newParentId)
        {
            // ...and the destination parent (so they cannot relocate a subtree out of their branch).
            await branchGuard.EnsureCanManageAsync(newParentId, cancellationToken);

            if (await repository.FindByIdAsync(newParentId, cancellationToken) is null)
                return Result.NotFound("New parent department not found.");

            // Cycle guard: the new parent must not be inside the moved department's own subtree
            // (IsInBranch is inclusive of self, so this also rejects newParent == department).
            if (await closure.IsInBranchAsync(department.Id, newParentId, cancellationToken))
                return Result.Conflict("A department cannot be moved under itself or one of its descendants.");
        }
        else
        {
            await branchGuard.EnsureFullScopeAsync(cancellationToken);
        }

        department.Reparent(command.NewParentId, DateTimeOffset.UtcNow);
        await repository.ReparentAsync(department, cancellationToken); // persists the move + closure atomically

        return Result.Success(department.ToDto());
    }
}
