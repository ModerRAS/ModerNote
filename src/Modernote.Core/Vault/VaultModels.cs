using System;

namespace Modernote.Core.Vault;

public sealed record TagDto(Guid Id, string Name);

public sealed record LinkDto(
    Guid Id,
    Guid FromObjectId,
    Guid? ToObjectId,
    string LinkKind,
    string Target);
