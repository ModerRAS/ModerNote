using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Modernote.Core.Model;
using Modernote.Core.Xml;

namespace Modernote.Desktop.Editor;

public sealed class EditorHost
{
    private readonly StackPanel _container;
    public ObservableCollection<Block> Blocks { get; } = new();
    public string BlockKindTitle { get; set; } = "Untitled";
    public int CurrentIndex { get; set; } = -1;

    public EditorHost(StackPanel container)
    {
        _container = container;
        Blocks.CollectionChanged += (_, _) => Rebuild();
    }

    public void LoadDocument(DocumentRoot doc)
    {
        Blocks.Clear();
        foreach (var b in doc.Children)
            Blocks.Add(b);
    }

    public DocumentRoot SaveDocument() => new()
    {
        Version = 1,
        Children = Blocks.ToList()
    };

    public void AddBlock(Block block, int afterIndex = -1)
    {
        if (afterIndex < 0 || afterIndex >= Blocks.Count)
            Blocks.Add(block);
        else
            Blocks.Insert(afterIndex + 1, block);
    }

    public void RemoveBlock(int index)
    {
        if (index >= 0 && index < Blocks.Count)
            Blocks.RemoveAt(index);
    }

    public void MoveBlockUp(int index)
    {
        if (index <= 0 || index >= Blocks.Count) return;
        Blocks.Move(index, index - 1);
        CurrentIndex = index - 1;
    }

    public void MoveBlockDown(int index)
    {
        if (index < 0 || index >= Blocks.Count - 1) return;
        Blocks.Move(index, index + 1);
        CurrentIndex = index + 1;
    }

    private void Rebuild()
    {
        _container.Children.Clear();
        for (int i = 0; i < Blocks.Count; i++)
        {
            var control = BuildBlockControl(Blocks[i]);
            var captured = i;
            control.GotFocus += (_, _) => CurrentIndex = captured;
            _container.Children.Add(control);
        }
    }

    private static Control BuildBlockControl(Block b)
    {
        // Try inline editor first (for editable block types)
        var editor = BlockEditorFactory.CreateInlineEditor(b, () => { });
        if (editor != null)
            return editor;

        // Fall back to read-only rendering for media blocks
        return RenderReadOnly(b);
    }

    private static Control RenderReadOnly(Block b) => b switch
    {
        HeadingBlock h => new TextBlock
        {
            Text = h.Text,
            FontWeight = FontWeight.Bold,
            FontSize = h.Level switch { 1 => 28, 2 => 24, 3 => 20, _ => 18 },
            Margin = new Thickness(0, 8)
        },
        ParagraphBlock p => new TextBlock
        {
            Text = p.Text,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4)
        },
        QuoteBlock q => new TextBlock
        {
            Text = q.Text,
            FontStyle = FontStyle.Italic,
            Margin = new Thickness(8, 4)
        },
        TodoBlock t => new CheckBox
        {
            Content = t.Text,
            IsChecked = t.Checked,
            Margin = new Thickness(0, 4)
        },
        CodeBlock c => new TextBlock
        {
            Text = c.Code,
            FontFamily = new FontFamily("Consolas, monospace"),
            Background = Brushes.LightGray,
            Padding = new Thickness(8),
            Margin = new Thickness(0, 4)
        },
        HorizontalRuleBlock => new Border
        {
            Height = 1,
            Background = Brushes.Gray,
            Margin = new Thickness(0, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch
        },
        ImageBlock i => new TextBlock
        {
            Text = $"[Image: {i.Source}]",
            Margin = new Thickness(0, 4)
        },
        VideoBlock v => new TextBlock
        {
            Text = $"[Video: {v.Source}]",
            Margin = new Thickness(0, 4)
        },
        AudioBlock a => new TextBlock
        {
            Text = $"[Audio: {a.Source}]",
            Margin = new Thickness(0, 4)
        },
        PdfBlock p => new TextBlock
        {
            Text = $"[PDF: {p.Source}]",
            Margin = new Thickness(0, 4)
        },
        FileBlock f => new TextBlock
        {
            Text = $"[File: {f.Source}]",
            Margin = new Thickness(0, 4)
        },
        _ => new TextBlock
        {
            Text = $"[Block: {b.GetType().Name}]",
            Margin = new Thickness(0, 4)
        }
    };
}
