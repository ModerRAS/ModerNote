using System.Collections.Generic;
using System.Linq;
using Modernote.Core.Model;
using Xunit;

namespace Modernote.Core.Tests;

public class DocumentModelTests
{
    [Fact]
    public void Heading_BlockType_Correct_ForAllLevels()
    {
        Assert.Equal(BlockType.H1, new HeadingBlock(1, "x").Type);
        Assert.Equal(BlockType.H3, new HeadingBlock(3, "x").Type);
        Assert.Equal(BlockType.H6, new HeadingBlock(6, "x").Type);
    }

    [Fact]
    public void Paragraph_BlockType_IsParagraph()
    {
        Assert.Equal(BlockType.Paragraph, new ParagraphBlock("hello").Type);
    }

    [Fact]
    public void Todo_TracksChecked()
    {
        var t = new TodoBlock("buy milk", true);
        Assert.True(t.Checked);
        Assert.Equal(BlockType.Todo, t.Type);
    }

    [Fact]
    public void Details_CanNestBlocks()
    {
        var inner = new[] { new ParagraphBlock("inner") };
        var d = new DetailsBlock("title", inner);
        Assert.Equal("title", d.Title);
        Assert.Single(d.Children);
        Assert.Equal(BlockType.Paragraph, d.Children[0].Type);
    }

    [Fact]
    public void Table_HoldsRows()
    {
        var rows = new List<IReadOnlyList<string>>
        {
            new[] { "A", "B" },
            new[] { "1", "2" }
        };
        var t = new TableBlock(rows);
        Assert.Equal(2, t.Rows.Count);
        Assert.Equal(2, t.Rows[0].Count);
    }

    [Fact]
    public void DocumentRoot_HoldsVersionAndChildren()
    {
        var doc = new DocumentRoot
        {
            Version = 1,
            Children = new Block[] { new ParagraphBlock("x") }
        };
        Assert.Equal(1, doc.Version);
        Assert.Single(doc.Children);
    }

    [Fact]
    public void AllBlockTypes_PresentInEnum()
    {
        var values = System.Enum.GetValues<BlockType>();
        Assert.Equal(19, values.Length);
    }
}
