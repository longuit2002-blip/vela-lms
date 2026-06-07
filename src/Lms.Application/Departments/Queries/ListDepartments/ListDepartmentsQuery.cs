using Ardalis.Result;
using Lms.Application.Authorization;
using Lms.Application.Departments.Dtos;
using Mediator;

namespace Lms.Application.Departments.Queries.ListDepartments;

/// <summary>Lists the current tenant's departments (flat tree nodes).</summary>
public sealed record ListDepartmentsQuery : IRequest<Result<IReadOnlyList<DepartmentDto>>>, IRequirePermission
{
    public string Permission => Permissions.Departments.Read;
}
