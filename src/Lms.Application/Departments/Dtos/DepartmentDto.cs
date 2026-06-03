using Lms.Domain.Departments;

namespace Lms.Application.Departments.Dtos;

/// <summary>Read model for a department. The tree is conveyed as flat nodes; clients derive the
/// hierarchy from <see cref="ParentId"/> (null = a root node).</summary>
public sealed record DepartmentDto(Guid Id, Guid? ParentId, string Name, DateTimeOffset CreatedAt);

/// <summary>Manual mapping (no AutoMapper) from the aggregate to its DTO.</summary>
public static class DepartmentMappings
{
    public static DepartmentDto ToDto(this Department department) =>
        new(department.Id, department.ParentId, department.Name, department.CreatedAt);
}
