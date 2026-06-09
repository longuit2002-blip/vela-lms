namespace Lms.Domain.Publishing;

/// <summary>Publish lifecycle of a <see cref="Publication"/>.</summary>
public enum PublicationStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2,
}
