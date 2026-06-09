using Ardalis.Result.AspNetCore;
using Lms.Application.Learning.Commands.CompleteLesson;
using Lms.Application.Learning.Dtos;
using Lms.Application.Learning.Queries.GetEnrolledCourseDetail;
using Lms.Application.Learning.Queries.GetMyEnrollments;
using Mediator;

namespace Lms.Api.Endpoints;

public static class LearningEndpoints
{
    public static IEndpointRouteBuilder MapLearningEndpoints(this IEndpointRouteBuilder app)
    {
        var me = app.MapGroup("/api/v1/me").WithTags("Learning").RequireAuthorization();

        me.MapGet("/enrollments", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMyEnrollmentsQuery(), ct)).ToMinimalApiResult())
            .WithName("GetMyEnrollments")
            .Produces<IReadOnlyList<EnrollmentSummaryDto>>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        var enrollments = app.MapGroup("/api/v1/enrollments").WithTags("Learning").RequireAuthorization();

        enrollments.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetEnrolledCourseDetailQuery(id), ct)).ToMinimalApiResult())
            .WithName("GetEnrolledCourseDetail")
            .Produces<EnrolledCourseDetailDto>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        enrollments.MapPost("/{enrollmentId:guid}/lessons/{lessonId:guid}/complete",
            async (Guid enrollmentId, Guid lessonId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new CompleteLessonCommand(enrollmentId, lessonId), ct)).ToMinimalApiResult())
            .WithName("CompleteLesson")
            .Produces<CompleteLessonResultDto>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
