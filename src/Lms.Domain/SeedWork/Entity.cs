namespace Lms.Domain.SeedWork;

/// <summary>
/// Base class for domain entities. Identity-based equality on <see cref="Id"/>.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; }

    public override bool Equals(object? obj) =>
        obj is Entity other
        && GetType() == other.GetType()
        && Id != default
        && Id == other.Id;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
