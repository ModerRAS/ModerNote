using System;
using Modernote.Core.Exceptions;
using Xunit;

namespace Modernote.Core.Tests;

public class ExceptionsTests
{
    [Fact]
    public void DocumentFormatException_HasMessage()
    {
        var ex = new DocumentFormatException("bad xml");
        Assert.Equal("bad xml", ex.Message);
    }

    [Fact]
    public void DocumentFormatException_WithInner()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new DocumentFormatException("outer", inner);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void VaultNotFoundException_StoresPath()
    {
        var ex = new VaultNotFoundException("C:/my-vault");
        Assert.Equal("C:/my-vault", ex.Path);
        Assert.Contains("C:/my-vault", ex.Message);
    }

    [Fact]
    public void ObjectNotFoundException_StoresId()
    {
        var id = Guid.NewGuid();
        var ex = new ObjectNotFoundException(id);
        Assert.Equal(id, ex.ObjectId);
        Assert.Contains(id.ToString(), ex.Message);
    }

    [Fact]
    public void ExtractionException_StoresFilePath()
    {
        var ex = new ExtractionException("test.pdf", "extraction failed");
        Assert.Equal("test.pdf", ex.FilePath);
    }

    [Fact]
    public void ExtractionException_WithInner()
    {
        var inner = new InvalidOperationException("io");
        var ex = new ExtractionException("test.pdf", "failed", inner);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void CoreException_IsAbstract()
    {
        Assert.True(typeof(CoreException).IsAbstract);
    }
}
