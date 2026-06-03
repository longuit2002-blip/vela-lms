using Lms.Domain.Positions;

namespace Lms.Application.Positions.Dtos;

/// <summary>Read model for a position (flat org-wide job title).</summary>
public sealed record PositionDto(Guid Id, string Name, DateTimeOffset CreatedAt);

/// <summary>Manual mapping (no AutoMapper) from the aggregate to its DTO.</summary>
public static class PositionMappings
{
    public static PositionDto ToDto(this Position position) =>
        new(position.Id, position.Name, position.CreatedAt);
}
