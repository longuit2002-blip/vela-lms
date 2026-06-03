using Ardalis.Result;
using Lms.Application.Authorization;
using Mediator;

namespace Lms.Application.Departments.Commands.DeleteDepartment;

public sealed record DeleteDepartmentCommand(Guid DepartmentId) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.Departments.Manage;
}
