using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Modernote.Core.Model;

namespace Modernote.Desktop.Editor;

/// <summary>
/// Creates inline editor controls for blocks that support text editing.
/// Returns null for media blocks (Image, Video, Audio, Pdf, File, HorizontalRule).
/// </summary>
public static class BlockEditorFactory
{
    /// <summary>Create an inline editor for a block. Returns null for media blocks.</summary>
    public static Control? CreateInlineEditor(Block block, Action onChanged)
    {
        return block switch
        {
            HeadingBlock h => CreateHeadingEditor(h, onChanged),
            ParagraphBlock p => CreateParagraphEditor(p, onChanged),
            TodoBlock t => CreateTodoEditor(t, onChanged),
            CodeBlock c => CreateCodeEditor(c, onChanged),
            QuoteBlock q => CreateQuoteEditor(q, onChanged),
            HorizontalRuleBlock => null,
            _ => null
        };
    }

    private static TextBox CreateHeadingEditor(HeadingBlock h, Action onChanged)
    {
        var tb = new TextBox
        {
            Text = h.Text,
            FontSize = h.Level switch { 1 => 28, 2 => 24, 3 => 20, _ => 18 },
            FontWeight = FontWeight.Bold
        };
        tb.LostFocus += (_, _) =>
        {
            h.GetType().GetProperty("Text")?.SetValue(h, tb.Text);
            onChanged();
        };
        return tb;
    }

    private static TextBox CreateParagraphEditor(ParagraphBlock p, Action onChanged)
    {
        var tb = new TextBox
        {
            Text = p.Text,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap
        };
        tb.LostFocus += (_, _) =>
        {
            p.GetType().GetProperty("Text")?.SetValue(p, tb.Text);
            onChanged();
        };
        return tb;
    }

    private static CheckBox CreateTodoEditor(TodoBlock t, Action onChanged)
    {
        var cb = new CheckBox
        {
            Content = t.Text,
            IsChecked = t.Checked
        };
        cb.IsCheckedChanged += (_, _) =>
        {
            t.GetType().GetProperty("Checked")?.SetValue(t, cb.IsChecked);
            onChanged();
        };
        return cb;
    }

    private static TextBox CreateCodeEditor(CodeBlock c, Action onChanged)
    {
        var tb = new TextBox
        {
            Text = c.Code,
            FontFamily = new FontFamily("Consolas, monospace"),
            AcceptsReturn = true
        };
        tb.LostFocus += (_, _) =>
        {
            c.GetType().GetProperty("Code")?.SetValue(c, tb.Text);
            onChanged();
        };
        return tb;
    }

    private static TextBox CreateQuoteEditor(QuoteBlock q, Action onChanged)
    {
        var tb = new TextBox
        {
            Text = q.Text,
            FontStyle = FontStyle.Italic,
            AcceptsReturn = true
        };
        tb.LostFocus += (_, _) =>
        {
            q.GetType().GetProperty("Text")?.SetValue(q, tb.Text);
            onChanged();
        };
        return tb;
    }
}
