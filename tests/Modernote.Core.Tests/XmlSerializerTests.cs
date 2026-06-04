using System.Collections.Generic;
using System.Linq;
using Modernote.Core.Model;
using Modernote.Core.Xml;
using Modernote.Core.Exceptions;
using Xunit;

namespace Modernote.Core.Tests;

public class XmlSerializerTests
{
    private const string Sample = @"<?xml version=""1.0"" encoding=""utf-8""?>
<document version=""1"">
  <h1>Title</h1>
  <p>Paragraph</p>
  <todo checked=""true"">Buy milk</todo>
  <code language=""csharp"">Console.WriteLine();</code>
  <image src=""photo.jpg""/>
  <hr/>
</document>";

    [Fact]
    public void Parse_ReadsBasicBlocks()
    {
        var doc = XmlNoteSerializer.Parse(Sample);
        Assert.Equal(1, doc.Version);
        Assert.Equal(6, doc.Children.Count);
        Assert.IsType<HeadingBlock>(doc.Children[0]);
        Assert.Equal("Title", ((HeadingBlock)doc.Children[0]).Text);
        Assert.IsType<ParagraphBlock>(doc.Children[1]);
        Assert.IsType<TodoBlock>(doc.Children[2]);
        Assert.True(((TodoBlock)doc.Children[2]).Checked);
        Assert.IsType<CodeBlock>(doc.Children[3]);
        Assert.Equal("csharp", ((CodeBlock)doc.Children[3]).Language);
        Assert.IsType<ImageBlock>(doc.Children[4]);
        Assert.IsType<HorizontalRuleBlock>(doc.Children[5]);
    }

    [Fact]
    public void Serialize_ProducesParseableXml()
    {
        var doc = new DocumentRoot
        {
            Version = 1,
            Children = new Block[] { new HeadingBlock(1, "T"), new ParagraphBlock("P") }
        };
        var xml = XmlNoteSerializer.Serialize(doc);
        Assert.Contains("<h1>T</h1>", xml);
        Assert.Contains("<p>P</p>", xml);
        Assert.Contains("version=\"1\"", xml);
        // Round trip
        var reparsed = XmlNoteSerializer.Parse(xml);
        Assert.Equal(2, reparsed.Children.Count);
    }

    [Fact]
    public void RoundTrip_AllBlockTypes()
    {
        var blocks = new Block[]
        {
            new HeadingBlock(1, "H1"),
            new HeadingBlock(6, "H6"),
            new ParagraphBlock("P"),
            new QuoteBlock("Q"),
            new CodeBlock("code", "csharp"),
            new TodoBlock("task", true),
            new ImageBlock("img.png", "cap", 800),
            new VideoBlock("v.mp4"),
            new AudioBlock("a.mp3"),
            new PdfBlock("d.pdf"),
            new FileBlock("f.zip"),
            new HorizontalRuleBlock(),
            new TableBlock(new List<IReadOnlyList<string>>
            {
                new[] { "A", "B" },
                new[] { "1", "2" }
            }),
            new DetailsBlock("title", new Block[] { new ParagraphBlock("inner") }),
            new CustomBlock("mytype", "src.dat")
        };
        var doc = new DocumentRoot { Version = 1, Children = blocks };
        var xml = XmlNoteSerializer.Serialize(doc);
        var parsed = XmlNoteSerializer.Parse(xml);
        Assert.Equal(blocks.Length, parsed.Children.Count);
    }

    [Fact]
    public void Parse_MalformedXml_Throws()
    {
        Assert.Throws<DocumentFormatException>(() =>
            XmlNoteSerializer.Parse("<document><p>missing close"));
    }

    [Fact]
    public void Parse_EmptyString_Throws()
    {
        Assert.Throws<DocumentFormatException>(() => XmlNoteSerializer.Parse(""));
    }

    [Fact]
    public void Parse_WrongRoot_Throws()
    {
        Assert.Throws<DocumentFormatException>(() =>
            XmlNoteSerializer.Parse("<wrong><p>x</p></wrong>"));
    }

    [Fact]
    public void Parse_NestedDetails_Works()
    {
        var xml = @"<?xml version=""1.0""?>
<document version=""1"">
  <details title=""section"">
    <p>inner</p>
    <image src=""i.jpg""/>
  </details>
</document>";
        var doc = XmlNoteSerializer.Parse(xml);
        var details = Assert.IsType<DetailsBlock>(doc.Children[0]);
        Assert.Equal("section", details.Title);
        Assert.Equal(2, details.Children.Count);
    }
}
