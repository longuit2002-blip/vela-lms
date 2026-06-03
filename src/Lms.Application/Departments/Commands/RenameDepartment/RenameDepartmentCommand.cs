using Ardalis.Result;
using Lms.Application.Authorization;
using Lms.Application.Departments.Dtos;
using Mediator;

namespace Lms.Application.Departments.Commands.RenameDepartment;

public sealed record RenameDepartmentCommand(Guid DepartmentId, string Name)
    : IRequest<Result<DepartmentDto>>, IRequirePermission
{
    public string Permission => Permissions.Departments.Manage;
}
