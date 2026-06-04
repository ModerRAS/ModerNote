namespace Modernote.Protocol;

/// <summary>Snapshot of a vault object. Immutable, value-equal.</summary>
public sealed record ObjectDto(
    Guid Id,
    ObjectKind Kind,
    string LogicalPath,
    string DisplayName,
    string ContentHash,
    string Mime,
    long SizeBytes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
