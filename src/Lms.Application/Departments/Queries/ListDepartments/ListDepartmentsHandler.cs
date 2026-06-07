using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Departments.Dtos;
using Mediator;

namespace Lms.Application.Departments.Queries.ListDepartments;

public sealed class ListDepartmentsHandler(IDepartmentRepository repository, ITenantContext tenant)
    : IRequestHandler<ListDepartmentsQuery, Result<IReadOnlyList<DepartmentDto>>>
{
    public async ValueTask<Result<IReadOnlyList<DepartmentDto>>> Handle(ListDepartmentsQuery query, CancellationToken cancellationToken)
    {
        if (tenant.OrganizationId == Guid.Empty)
            return Result.Unauthorized();

        var departments = await repository.ListByOrganizationAsync(cancellationToken);
        IReadOnlyList<DepartmentDto> dtos = [.. departments.Select(d => d.ToDto())];
        return Result.Success(dtos);
    }
}
