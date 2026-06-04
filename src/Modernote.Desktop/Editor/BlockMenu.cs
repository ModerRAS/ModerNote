using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Modernote.Core.Model;

namespace Modernote.Desktop.Editor;

public static class BlockMenu
{
    public static MenuFlyout CreateAddBlockMenu(Action<BlockType> onSelect)
    {
        var menu = new MenuFlyout();
        foreach (var (label, type) in GetBlockOptions())
        {
            var capturedType = type;
            var item = new MenuItem { Header = label };
            item.Click += (_, _) => onSelect(capturedType);
            menu.Items.Add(item);
        }
        return menu;
    }

    public static Block CreateBlock(BlockType type, string defaultText = "") => type switch
    {
        BlockType.H1 => new HeadingBlock(1, defaultText),
        BlockType.H2 => new HeadingBlock(2, defaultText),
        BlockType.H3 => new HeadingBlock(3, defaultText),
        BlockType.Paragraph => new ParagraphBlock(defaultText),
        BlockType.Todo => new TodoBlock(defaultText, false),
        BlockType.Code => new CodeBlock(defaultText, ""),
        BlockType.Quote => new QuoteBlock(defaultText),
        _ => new ParagraphBlock(defaultText)
    };

    public static IEnumerable<(string Label, BlockType Type)> GetBlockOptions() => new[]
    {
        ("Heading 1", BlockType.H1),
        ("Heading 2", BlockType.H2),
        ("Heading 3", BlockType.H3),
        ("Paragraph", BlockType.Paragraph),
        ("Todo", BlockType.Todo),
        ("Code", BlockType.Code),
        ("Quote", BlockType.Quote)
    };
}
