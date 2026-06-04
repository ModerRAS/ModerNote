using System;

namespace Modernote.Core.Exceptions;

/// <summary>Thrown when text extraction from a file fails.</summary>
public sealed class ExtractionException : Exception
{
    public string FilePath { get; }

    public ExtractionException(string filePath, string message) : base(message)
    {
        FilePath = filePath;
    }

    public ExtractionException(string filePath, string message, Exception inner) : base(message, inner)
    {
        FilePath = filePath;
    }
}
