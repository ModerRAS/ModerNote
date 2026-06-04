using System.Linq;
using Modernote.Core.Exceptions;
using Modernote.Core.Model;
using Modernote.Core.Xml;
using Xunit;

namespace Modernote.Core.Tests;

public class XmlNoteSerializerTests
{
    [Fact]
    public void RoundTrip_AllBlockTypes_PreservesContent()
    {
        var original = new DocumentRoot
        {
            Version = 1,
            Children = new Block[]
            {
                new HeadingBlock(1, "Title"),
                new ParagraphBlock("Body text"),
                new ImageBlock("photo.jpg", "A photo", 800),
                new VideoBlock("movie.mp4"),
                new AudioBlock("sound.mp3"),
                new PdfBlock("doc.pdf"),
                new FileBlock("code.zip"),
                new QuoteBlock("Quoted"),
                new CodeBlock("Console.WriteLine();", "csharp"),
                new TableBlock(new[] {
                    new[] { "A", "B" },
                    new[] { "1", "2" }
                }),
                new TodoBlock("Buy milk", true),
                new DetailsBlock("Section", new Block[] { new ParagraphBlock("inner") }),
                new HorizontalRuleBlock(),
                new CustomBlock("kicad-preview", "main.kicad_sch"),
            }
        };

        var xml = XmlNoteSerializer.Serialize(original);
        var roundtripped = XmlNoteSerializer.Parse(xml);

        Assert.Equal(1, roundtripped.Version);
        Assert.Equal(original.Children.Count, roundtripped.Children.Count);
        Assert.IsType<HeadingBlock>(roundtripped.Children[0]);
        Assert.Equal("Title", ((HeadingBlock)roundtripped.Children[0]).Text);
        Assert.IsType<ImageBlock>(roundtripped.Children[2]);
        Assert.Equal("photo.jpg", ((ImageBlock)roundtripped.Children[2]).Source);
        Assert.Equal(800, ((ImageBlock)roundtripped.Children[2]).Width);
    }

    [Fact]
    public void Parse_Headings_AllSixLevels()
    {
        var xml = "<document version=\"1\"><h1>a</h1><h2>b</h2><h3>c</h3><h4>d</h4><h5>e</h5><h6>f</h6></document>";
        var doc = XmlNoteSerializer.Parse(xml);
        Assert.Equal(6, doc.Children.Count);
        Assert.Equal(BlockType.H1, doc.Children[0].Type);
        Assert.Equal(BlockType.H6, doc.Children[5].Type);
    }

    [Fact]
    public void Parse_Paragraph_StripsNone()
    {
        var doc = XmlNoteSerializer.Parse("<document version=\"1\"><p>hello world</p></document>");
        Assert.IsType<ParagraphBlock>(doc.Children[0]);
        Assert.Equal("hello world", ((ParagraphBlock)doc.Children[0]).Text);
    }

    [Fact]
    public void Parse_Todo_ReadsCheckedAttribute()
    {
        var doc = XmlNoteSerializer.Parse("<document version=\"1\"><todo checked=\"true\">x</todo></document>");
        var todo = (TodoBlock)doc.Children[0];
        Assert.True(todo.Checked);
    }

    [Fact]
    public void Parse_Code_ReadsLanguageAttribute()
    {
        var doc = XmlNoteSerializer.Parse("<document version=\"1\"><code language=\"python\">x</code></document>");
        var code = (CodeBlock)doc.Children[0];
        Assert.Equal("python", code.Language);
    }

    [Fact]
    public void Parse_Details_NestsChildren()
    {
        var doc = XmlNoteSerializer.Parse("<document version=\"1\"><details title=\"t\"><p>inner</p></details></document>");
        var d = (DetailsBlock)doc.Children[0];
        Assert.Equal("t", d.Title);
        Assert.Single(d.Children);
        Assert.IsType<ParagraphBlock>(d.Children[0]);
    }

    [Fact]
    public void Parse_MalformedXml_ThrowsFormatException()
    {
        Assert.Throws<DocumentFormatException>(() => XmlNoteSerializer.Parse("<document><p>missing close"));
    }

    [Fact]
    public void Parse_WrongRoot_ThrowsFormatException()
    {
        Assert.Throws<DocumentFormatException>(() => XmlNoteSerializer.Parse("<note>x</note>"));
    }

    [Fact]
    public void Parse_UnknownElement_ThrowsFormatException()
    {
        Assert.Throws<DocumentFormatException>(() => XmlNoteSerializer.Parse("<document version=\"1\"><mystery>x</mystery></document>"));
    }

    [Fact]
    public void Serialize_OutputContainsVersionAttribute()
    {
        var doc = new DocumentRoot { Children = new Block[] { new ParagraphBlock("x") } };
        var xml = XmlNoteSerializer.Serialize(doc);
        Assert.Contains("version=\"1\"", xml);
    }

    [Fact]
    public void Serialize_TodoCheckedFalse_OutputsFalseString()
    {
        var doc = new DocumentRoot { Children = new Block[] { new TodoBlock("x", false) } };
        var xml = XmlNoteSerializer.Serialize(doc);
        Assert.Contains("checked=\"false\"", xml);
    }
}
