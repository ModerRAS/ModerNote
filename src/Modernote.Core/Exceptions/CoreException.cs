using System;

namespace Modernote.Core.Exceptions;

/// <summary>Base class for all Modernote Core exceptions.</summary>
public abstract class CoreException : Exception
{
    public CoreException() : base() { }
    public CoreException(string message) : base(message) { }
    public CoreException(string message, Exception inner) : base(message, inner) { }
}
