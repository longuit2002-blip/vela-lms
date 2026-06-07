using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Authorization;
using Lms.Application.Departments.Dtos;
using Mediator;

namespace Lms.Application.Departments.Commands.RenameDepartment;

public sealed class RenameDepartmentHandler(
    IDepartmentRepository repository,
    IDepartmentBranchGuard branchGuard)
    : IRequestHandler<RenameDepartmentCommand, Result<DepartmentDto>>
{
    public async ValueTask<Result<DepartmentDto>> Handle(RenameDepartmentCommand command, CancellationToken cancellationToken)
    {
        var department = await repository.FindByIdAsync(command.DepartmentId, cancellationToken);
        if (department is null)
            return Result.NotFound();

        await branchGuard.EnsureCanManageAsync(department.Id, cancellationToken);

        department.Rename(command.Name, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success(department.ToDto());
    }
}
