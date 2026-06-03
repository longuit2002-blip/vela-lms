using Ardalis.Result;
using Lms.Application.Authorization;
using Lms.Application.Departments.Dtos;
using Mediator;

namespace Lms.Application.Departments.Commands.CreateDepartment;

/// <summary>Creates a department, optionally under <paramref name="ParentId"/> (null = a root node).</summary>
public sealed record CreateDepartmentCommand(string Name, Guid? ParentId)
    : IRequest<Result<DepartmentDto>>, IRequirePermission
{
    public string Permission => Permissions.Departments.Manage;
}
