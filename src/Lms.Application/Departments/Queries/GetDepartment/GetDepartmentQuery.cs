using Ardalis.Result;
using Lms.Application.Authorization;
using Lms.Application.Departments.Dtos;
using Mediator;

namespace Lms.Application.Departments.Queries.GetDepartment;

public sealed record GetDepartmentQuery(Guid DepartmentId) : IRequest<Result<DepartmentDto>>, IRequirePermission
{
    public string Permission => Permissions.Departments.Read;
}
