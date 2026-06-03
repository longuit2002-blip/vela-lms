namespace Lms.Application.Abstractions;

/// <summary>
/// Generates database-sort-friendly UUID v7 identifiers. Implemented in Infrastructure
/// (Medo.Uuid7) so the Application/Domain layers stay free of the generation strategy.
/// </summary>
public interface IIdGenerator
{
    Guid NewId();
}
