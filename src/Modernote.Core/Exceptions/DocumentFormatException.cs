namespace Modernote.Core.Exceptions;

public class DocumentFormatException : Exception
{
    public DocumentFormatException(string message) : base(message) { }
    public DocumentFormatException(string message, Exception inner) : base(message, inner) { }
}
