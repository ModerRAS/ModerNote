namespace Modernote.Protocol;

/// <summary>Search result with snippet and score.</summary>
public sealed record SearchResultDto(
    ObjectDto Object,
    string Snippet,
    double Score
);
