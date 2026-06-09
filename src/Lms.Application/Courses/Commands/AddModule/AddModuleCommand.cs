using Ardalis.Result;
using Lms.Application.Authorization;
using Lms.Application.Courses.Dtos;
using Mediator;

namespace Lms.Application.Courses.Commands.AddModule;

/// <summary>Appends a module to a Draft course.</summary>
public sealed record AddModuleCommand(Guid CourseId, string Title)
    : IRequest<Result<ModuleDto>>, IRequirePermission
{
    public string Permission => Permissions.Courses.Update;
}
