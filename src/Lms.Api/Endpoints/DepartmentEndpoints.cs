using Ardalis.Result.AspNetCore;
using Lms.Application.Departments.Commands.CreateDepartment;
using Lms.Application.Departments.Commands.DeleteDepartment;
using Lms.Application.Departments.Commands.MoveDepartment;
using Lms.Application.Departments.Commands.RenameDepartment;
using Lms.Application.Departments.Dtos;
using Lms.Application.Departments.Queries.GetDepartment;
using Lms.Application.Departments.Queries.ListDepartments;
using Mediator;

namespace Lms.Api.Endpoints;

public static class DepartmentEndpoints
{
    public static IEndpointRouteBuilder MapDepartmentEndpoints(this IEndpointRouteBuilder app)
    {
        // .RequireAuthorization() gates on a valid token; per-action permissions (RBAC) and branch
        // scoping (ABAC) are enforced in the Mediator pipeline + handlers, surfacing as 403.
        var group = app.MapGroup("/api/v1/departments").WithTags("Departments").RequireAuthorization();

        group.MapGet("/", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new ListDepartmentsQuery(), cancellationToken)).ToMinimalApiResult())
            .WithName("ListDepartments")
            .Produces<IReadOnlyList<DepartmentDto>>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new GetDepartmentQuery(id), cancellationToken)).ToMinimalApiResult())
            .WithName("GetDepartment")
            .Produces<DepartmentDto>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (CreateDepartmentRequest request, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new CreateDepartmentCommand(request.Name, request.ParentId), cancellationToken)).ToMinimalApiResult())
            .WithName("CreateDepartment")
            .Produces<DepartmentDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}", async (Guid id, RenameDepartmentRequest request, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RenameDepartmentCommand(id, request.Name), cancellationToken)).ToMinimalApiResult())
            .WithName("RenameDepartment")
            .Produces<DepartmentDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/move", async (Guid id, MoveDepartmentRequest request, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new MoveDepartmentCommand(id, request.NewParentId), cancellationToken)).ToMinimalApiResult())
            .WithName("MoveDepartment")
            .Produces<DepartmentDto>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new DeleteDepartmentCommand(id), cancellationToken)).ToMinimalApiResult())
            .WithName("DeleteDepartment")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private sealed record CreateDepartmentRequest(string Name, Guid? ParentId);
    private sealed record RenameDepartmentRequest(string Name);
    private sealed record MoveDepartmentRequest(Guid? NewParentId);
}
