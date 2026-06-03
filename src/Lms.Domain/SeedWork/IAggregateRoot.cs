namespace Lms.Domain.SeedWork;

/// <summary>
/// Marks an entity as an aggregate root — the consistency boundary and the only
/// type repositories return for its aggregate.
/// </summary>
public interface IAggregateRoot;
