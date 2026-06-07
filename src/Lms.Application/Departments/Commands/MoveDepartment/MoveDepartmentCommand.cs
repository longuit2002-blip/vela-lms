using Ardalis.Result;
using Lms.Application.Authorization;
using Lms.Application.Departments.Dtos;
using Mediator;

namespace Lms.Application.Departments.Commands.MoveDepartment;

/// <summary>Reparents a department under <paramref name="NewParentId"/> (null = make it a root).</summary>
public sealed record MoveDepartmentCommand(Guid DepartmentId, Guid? NewParentId)
    : IRequest<Result<DepartmentDto>>, IRequirePermission
{
    public string Permission => Permissions.Departments.Manage;
}
