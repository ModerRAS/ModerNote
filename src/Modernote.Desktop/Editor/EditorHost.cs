using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
    public string? VaultRoot { get; set; }

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

    public string SerializeDocument()
    {
        return XmlNoteSerializer.Serialize(SaveDocument());
    }

    /// <summary>Alias for SerializeDocument, used by undo/redo.</summary>
    public string SerializeXml() => SerializeDocument();

    public void LoadXml(string xml)
    {
        var doc = XmlNoteSerializer.Parse(xml);
        LoadDocument(doc);
    }

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

    private Control BuildBlockControl(Block b)
    {
        // Try inline editor first (for editable block types)
        var editor = BlockEditorFactory.CreateInlineEditor(b, () => { });
        if (editor != null)
            return editor;

        // Fall back to read-only rendering for media blocks
        return RenderReadOnly(b);
    }

    private Control RenderReadOnly(Block b) => b switch
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
        ImageBlock i => RenderImage(i),
        VideoBlock v => RenderMedia(v.Source, "\U0001F3AC", "Video"),
        AudioBlock a => RenderMedia(a.Source, "\U0001F3B5", "Audio"),
        PdfBlock p => RenderMedia(p.Source, "\U0001F4D5", "PDF"),
        FileBlock f => RenderMedia(f.Source, "\U0001F4E6", "File"),
        _ => new TextBlock
        {
            Text = $"[Block: {b.GetType().Name}]",
            Margin = new Thickness(0, 4)
        }
    };

    private Control RenderImage(ImageBlock i)
    {
        if (VaultRoot == null)
            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Margin = new Thickness(0, 4),
                Children =
                {
                    new TextBlock { Text = "\U0001F5BC", FontSize = 24 },
                    new TextBlock { Text = i.Source, VerticalAlignment = VerticalAlignment.Center }
                }
            };

        var path = Path.Combine(VaultRoot, i.Source.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(path))
        {
            try
            {
                var img = new Avalonia.Controls.Image
                {
                    Source = new Avalonia.Media.Imaging.Bitmap(path),
                    MaxWidth = 600,
                    Stretch = Avalonia.Media.Stretch.Uniform,
                    Margin = new Thickness(0, 4)
                };
                return img;
            }
            catch
            {
                return new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Margin = new Thickness(0, 4),
                    Children =
                    {
                        new TextBlock { Text = "\U0001F5BC", FontSize = 24 },
                        new TextBlock { Text = i.Source, VerticalAlignment = VerticalAlignment.Center }
                    }
                };
            }
        }
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 4),
            Children =
            {
                new TextBlock { Text = "\U0001F5BC", FontSize = 24 },
                new TextBlock { Text = $"[Missing: {i.Source}]", Foreground = Brushes.Red, VerticalAlignment = VerticalAlignment.Center }
            }
        };
    }

    private static Control RenderMedia(string source, string emoji, string label)
    {
        var border = new Border
        {
            Background = Brushes.LightGray,
            Padding = new Thickness(8),
            Margin = new Thickness(0, 4),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = emoji, FontSize = 24 },
                    new TextBlock { Text = $"[{label}: {source}]", VerticalAlignment = VerticalAlignment.Center }
                }
            }
        };
        return border;
    }
}
