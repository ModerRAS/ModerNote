using System.Collections.Generic;

namespace Modernote.Core.Model;

/// <summary>The root of a document. Version enables future migration logic.</summary>
public sealed record DocumentRoot
{
    public int Version { get; init; } = 1;
    public IReadOnlyList<Block> Children { get; init; } = System.Array.Empty<Block>();
}
