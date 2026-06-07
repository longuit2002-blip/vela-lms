using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Departments.Dtos;
using Mediator;

namespace Lms.Application.Departments.Queries.GetDepartment;

public sealed class GetDepartmentHandler(IDepartmentRepository repository)
    : IRequestHandler<GetDepartmentQuery, Result<DepartmentDto>>
{
    public async ValueTask<Result<DepartmentDto>> Handle(GetDepartmentQuery query, CancellationToken cancellationToken)
    {
        var department = await repository.FindByIdAsync(query.DepartmentId, cancellationToken);
        return department is null
            ? Result.NotFound()
            : Result.Success(department.ToDto());
    }
}
