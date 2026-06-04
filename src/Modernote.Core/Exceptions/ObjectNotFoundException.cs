using System;

namespace Modernote.Core.Exceptions;

/// <summary>Thrown when a vault object is not found by its ID.</summary>
public sealed class ObjectNotFoundException : Exception
{
    public Guid ObjectId { get; }

    public ObjectNotFoundException(Guid id) : base($"Object {id} not found")
    {
        ObjectId = id;
    }
}
