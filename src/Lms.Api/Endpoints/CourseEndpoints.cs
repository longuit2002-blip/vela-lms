using Ardalis.Result.AspNetCore;
using Lms.Application.Courses.Commands.AddLesson;
using Lms.Application.Courses.Commands.AddModule;
using Lms.Application.Courses.Commands.CreateCourse;
using Lms.Application.Courses.Dtos;
using Lms.Application.Courses.Queries.GetCourse;
using Mediator;

namespace Lms.Api.Endpoints;

public static class CourseEndpoints
{
    public static IEndpointRouteBuilder MapCourseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/courses").WithTags("Courses").RequireAuthorization();

        group.MapPost("/", async (CreateCourseRequest request, ISender sender, CancellationToken ct) =>
            (await sender.Send(new CreateCourseCommand(request.Title, request.Slug), ct)).ToMinimalApiResult())
            .WithName("CreateCourse")
            .Produces<CourseDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetCourseQuery(id), ct)).ToMinimalApiResult())
            .WithName("GetCourse")
            .Produces<CourseDto>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{courseId:guid}/modules", async (Guid courseId, AddModuleRequest request, ISender sender, CancellationToken ct) =>
            (await sender.Send(new AddModuleCommand(courseId, request.Title), ct)).ToMinimalApiResult())
            .WithName("AddModule")
            .Produces<ModuleDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{courseId:guid}/modules/{moduleId:guid}/lessons",
            async (Guid courseId, Guid moduleId, AddLessonRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new AddLessonCommand(courseId, moduleId, request.Title, request.VideoUrl, request.DurationSeconds), ct))
                    .ToMinimalApiResult())
            .WithName("AddLesson")
            .Produces<LessonDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private sealed record CreateCourseRequest(string Title, string Slug);
    private sealed record AddModuleRequest(string Title);
    private sealed record AddLessonRequest(string Title, string VideoUrl, int DurationSeconds);
}
