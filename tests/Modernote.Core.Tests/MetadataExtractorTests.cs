using Modernote.Core.Import;
using Modernote.Protocol;
using Xunit;

namespace Modernote.Core.Tests;

public class MetadataExtractorTests
{
    [Theory]
    [InlineData("notes/note.xml", ObjectKind.XmlNote)]
    [InlineData("notes/note.html", ObjectKind.XmlNote)]
    [InlineData("notes/note.htm", ObjectKind.XmlNote)]
    [InlineData("assets/file.pdf", ObjectKind.Pdf)]
    [InlineData("assets/file.docx", ObjectKind.Docx)]
    [InlineData("assets/file.png", ObjectKind.Image)]
    [InlineData("assets/file.jpg", ObjectKind.Image)]
    [InlineData("assets/file.mp3", ObjectKind.Audio)]
    [InlineData("assets/file.wav", ObjectKind.Audio)]
    [InlineData("assets/file.mp4", ObjectKind.Video)]
    [InlineData("assets/file.mkv", ObjectKind.Video)]
    [InlineData("assets/file.txt", ObjectKind.Text)]
    [InlineData("assets/file.md", ObjectKind.Text)]
    [InlineData("assets/file.cs", ObjectKind.Code)]
    [InlineData("assets/file.rs", ObjectKind.Code)]
    [InlineData("assets/file.json", ObjectKind.Code)]
    public void DetectKind_CommonFiles_ReturnsCorrectKind(string logicalPath, ObjectKind expected)
    {
        var ext = System.IO.Path.GetExtension(logicalPath);
        Assert.Equal(expected, MetadataExtractor.DetectKind(logicalPath, "some" + ext));
    }

    [Theory]
    [InlineData(ObjectKind.XmlNote, ".xml", "text/xml")]
    [InlineData(ObjectKind.Image, ".png", "image/png")]
    [InlineData(ObjectKind.Image, ".jpg", "image/jpeg")]
    [InlineData(ObjectKind.Pdf, ".pdf", "application/pdf")]
    [InlineData(ObjectKind.Docx, ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData(ObjectKind.Audio, ".mp3", "audio/mpeg")]
    [InlineData(ObjectKind.Audio, ".wav", "audio/wav")]
    [InlineData(ObjectKind.Audio, ".ogg", "audio/ogg")]
    [InlineData(ObjectKind.Video, ".mp4", "video/mp4")]
    [InlineData(ObjectKind.Video, ".webm", "video/webm")]
    [InlineData(ObjectKind.Video, ".mov", "video/quicktime")]
    [InlineData(ObjectKind.Text, ".txt", "text/plain")]
    [InlineData(ObjectKind.Code, ".json", "application/json")]
    [InlineData(ObjectKind.Code, ".cs", "text/plain")]
    [InlineData(ObjectKind.Other, ".xyz", "application/octet-stream")]
    public void DetectMime_ReturnsCorrectMime(ObjectKind kind, string extension, string expected)
    {
        Assert.Equal(expected, MetadataExtractor.DetectMime(kind, "file" + extension));
    }
}
