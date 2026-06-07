using Lms.Application.Abstractions;
using Medo;

namespace Lms.Infrastructure.Identifiers;

/// <summary>
/// UUID v7 generator (Medo.Uuid7) producing database-sort-friendly GUIDs. Avoids
/// <c>Guid.CreateVersion7</c>, whose in-memory byte order is not big-endian and fragments
/// PostgreSQL <c>uuid</c> indexes.
/// </summary>
public sealed class Uuid7IdGenerator : IIdGenerator
{
    public Guid NewId() => Uuid7.NewUuid7().ToGuid();
}
