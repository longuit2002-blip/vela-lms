using Ardalis.Result.AspNetCore;
using Lms.Application.Publishing.Commands.AssignPublication;
using Lms.Application.Publishing.Commands.CreatePublication;
using Lms.Application.Publishing.Commands.PublishPublication;
using Lms.Application.Publishing.Dtos;
using Mediator;

namespace Lms.Api.Endpoints;

public static class PublicationEndpoints
{
    public static IEndpointRouteBuilder MapPublicationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/publications").WithTags("Publications").RequireAuthorization();

        group.MapPost("/", async (CreatePublicationRequest request, ISender sender, CancellationToken ct) =>
            (await sender.Send(new CreatePublicationCommand(request.CourseId, request.Title), ct)).ToMinimalApiResult())
            .WithName("CreatePublication")
            .Produces<PublicationDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/publish", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new PublishPublicationCommand(id), ct)).ToMinimalApiResult())
            .WithName("PublishPublication")
            .Produces<PublicationDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/assign", async (Guid id, AssignPublicationRequest request, ISender sender, CancellationToken ct) =>
            (await sender.Send(new AssignPublicationCommand(id, request.UserIds), ct)).ToMinimalApiResult())
            .WithName("AssignPublication")
            .Produces<AssignmentResultDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private sealed record CreatePublicationRequest(Guid CourseId, string Title);
    private sealed record AssignPublicationRequest(IReadOnlyList<Guid> UserIds);
}
