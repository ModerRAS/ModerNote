using System.IO;
using System.Text;
using Modernote.Core.Extraction;
using Modernote.Protocol;
using Xunit;

namespace Modernote.Core.Tests;

public class TextExtractorTests
{
    [Fact]
    public void Extract_PlainText_ReturnsContent()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "hello world");
        var result = TextExtractor.Extract(ObjectKind.Text, path);
        Assert.Equal("hello world", result.Body);
        Assert.Equal("plain_text", result.Extractor);
        File.Delete(path);
    }

    [Fact]
    public void Extract_XmlNote_StripsTags()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "<document version=\"1\"><h1>Title</h1><p>Body text</p></document>");
        var result = TextExtractor.Extract(ObjectKind.XmlNote, path);
        Assert.Contains("Title", result.Body);
        Assert.Contains("Body text", result.Body);
        Assert.DoesNotContain("<", result.Body);
        File.Delete(path);
    }

    [Fact]
    public void Extract_Image_ReturnsEmpty()
    {
        var path = Path.GetTempFileName();
        File.WriteAllBytes(path, new byte[] { 0xFF, 0xD8, 0xFF });
        var result = TextExtractor.Extract(ObjectKind.Image, path);
        Assert.Equal(string.Empty, result.Body);
        Assert.Equal("metadata_only", result.Extractor);
        File.Delete(path);
    }

    [Fact]
    public void Extract_MissingFile_ReturnsFailed()
    {
        var result = TextExtractor.Extract(ObjectKind.Text, "Z:/nonexistent.txt");
        Assert.NotNull(result.Error);
    }
}
