using Lms.Domain.Publishing;

namespace Lms.Application.Publishing.Dtos;

/// <summary>Read model for a publication.</summary>
public sealed record PublicationDto(Guid Id, string Kind, Guid ContentId, string Title, string Status, DateTimeOffset? PublishedAt);

/// <summary>Outcome of an assign call: how many enrollments were created vs. skipped (already enrolled).</summary>
public sealed record AssignmentResultDto(Guid PublicationId, int Enrolled, int Skipped);

public static class PublicationMappings
{
    public static PublicationDto ToDto(this Publication publication) =>
        new(publication.Id, publication.Kind, publication.ContentId, publication.Title, publication.Status.ToString(), publication.PublishedAt);
}
