using Ardalis.Result.AspNetCore;
using Lms.Application.Positions.Commands.CreatePosition;
using Lms.Application.Positions.Commands.DeletePosition;
using Lms.Application.Positions.Commands.RenamePosition;
using Lms.Application.Positions.Dtos;
using Lms.Application.Positions.Queries.ListPositions;
using Mediator;

namespace Lms.Api.Endpoints;

public static class PositionEndpoints
{
    public static IEndpointRouteBuilder MapPositionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/positions").WithTags("Positions").RequireAuthorization();

        group.MapGet("/", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new ListPositionsQuery(), cancellationToken)).ToMinimalApiResult())
            .WithName("ListPositions")
            .Produces<IReadOnlyList<PositionDto>>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/", async (CreatePositionRequest request, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new CreatePositionCommand(request.Name), cancellationToken)).ToMinimalApiResult())
            .WithName("CreatePosition")
            .Produces<PositionDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPatch("/{id:guid}", async (Guid id, RenamePositionRequest request, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RenamePositionCommand(id, request.Name), cancellationToken)).ToMinimalApiResult())
            .WithName("RenamePosition")
            .Produces<PositionDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new DeletePositionCommand(id), cancellationToken)).ToMinimalApiResult())
            .WithName("DeletePosition")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private sealed record CreatePositionRequest(string Name);
    private sealed record RenamePositionRequest(string Name);
}
