namespace Modernote.Core.Exceptions;

/// <summary>Thrown when a folder path is invalid (e.g., contains .. or absolute path).</summary>
public sealed class InvalidFolderException : CoreException
{
    public InvalidFolderException(string path)
        : base($"Invalid folder path: '{path}'")
    { }
}
