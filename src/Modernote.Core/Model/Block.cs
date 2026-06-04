using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Modernote.Core.Model;

[JsonDerivedType(typeof(HeadingBlock), "heading")]
[JsonDerivedType(typeof(ParagraphBlock), "paragraph")]
[JsonDerivedType(typeof(QuoteBlock), "quote")]
[JsonDerivedType(typeof(CodeBlock), "code")]
[JsonDerivedType(typeof(TodoBlock), "todo")]
[JsonDerivedType(typeof(ImageBlock), "image")]
[JsonDerivedType(typeof(VideoBlock), "video")]
[JsonDerivedType(typeof(AudioBlock), "audio")]
[JsonDerivedType(typeof(PdfBlock), "pdf")]
[JsonDerivedType(typeof(FileBlock), "file")]
[JsonDerivedType(typeof(HorizontalRuleBlock), "hr")]
[JsonDerivedType(typeof(TableBlock), "table")]
[JsonDerivedType(typeof(DetailsBlock), "details")]
[JsonDerivedType(typeof(CustomBlock), "custom")]
public abstract record Block(BlockType Type)
{
    // Concrete record types follow. All have init-only properties.
}

public sealed record HeadingBlock : Block
{
    public required int Level { get; init; }   // 1-6
    public required string Text { get; init; }

    [SetsRequiredMembers]
    public HeadingBlock(int level, string text) : base(ToBlockType(level))
    {
        Level = level;
        Text = text;
    }

    private static BlockType ToBlockType(int level) => level switch
    {
        1 => BlockType.H1, 2 => BlockType.H2, 3 => BlockType.H3,
        4 => BlockType.H4, 5 => BlockType.H5, 6 => BlockType.H6,
        _ => BlockType.Paragraph
    };
}

public sealed record ParagraphBlock : Block
{
    public required string Text { get; init; }

    [SetsRequiredMembers]
    public ParagraphBlock(string text) : base(BlockType.Paragraph)
    {
        Text = text;
    }
}

public sealed record QuoteBlock : Block
{
    public required string Text { get; init; }

    [SetsRequiredMembers]
    public QuoteBlock(string text) : base(BlockType.Quote)
    {
        Text = text;
    }
}

public sealed record CodeBlock : Block
{
    public required string Code { get; init; }
    public string? Language { get; init; }

    [SetsRequiredMembers]
    public CodeBlock(string code, string? language = null) : base(BlockType.Code)
    {
        Code = code;
        Language = language;
    }
}

public sealed record TodoBlock : Block
{
    public required string Text { get; init; }
    public required bool Checked { get; init; }

    [SetsRequiredMembers]
    public TodoBlock(string text, bool @checked) : base(BlockType.Todo)
    {
        Text = text;
        Checked = @checked;
    }
}

public sealed record ImageBlock : Block
{
    public required string Source { get; init; }
    public string? Caption { get; init; }
    public int? Width { get; init; }

    [SetsRequiredMembers]
    public ImageBlock(string source, string? caption = null, int? width = null) : base(BlockType.Image)
    {
        Source = source;
        Caption = caption;
        Width = width;
    }
}

public sealed record VideoBlock : Block
{
    public required string Source { get; init; }

    [SetsRequiredMembers]
    public VideoBlock(string source) : base(BlockType.Video)
    {
        Source = source;
    }
}

public sealed record AudioBlock : Block
{
    public required string Source { get; init; }

    [SetsRequiredMembers]
    public AudioBlock(string source) : base(BlockType.Audio)
    {
        Source = source;
    }
}

public sealed record PdfBlock : Block
{
    public required string Source { get; init; }

    [SetsRequiredMembers]
    public PdfBlock(string source) : base(BlockType.Pdf)
    {
        Source = source;
    }
}

public sealed record FileBlock : Block
{
    public required string Source { get; init; }

    [SetsRequiredMembers]
    public FileBlock(string source) : base(BlockType.File)
    {
        Source = source;
    }
}

public sealed record HorizontalRuleBlock : Block
{
    public HorizontalRuleBlock() : base(BlockType.HorizontalRule) { }
}

public sealed record TableBlock : Block
{
    public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }

    [SetsRequiredMembers]
    public TableBlock(IReadOnlyList<IReadOnlyList<string>> rows) : base(BlockType.Table)
    {
        Rows = rows;
    }
}

public sealed record DetailsBlock : Block
{
    public required string Title { get; init; }
    public required IReadOnlyList<Block> Children { get; init; }

    [SetsRequiredMembers]
    public DetailsBlock(string title, IReadOnlyList<Block> children) : base(BlockType.Details)
    {
        Title = title;
        Children = children;
    }
}

public sealed record CustomBlock : Block
{
    public required string CustomType { get; init; }
    public string? Source { get; init; }
    public IReadOnlyDictionary<string, string>? Attributes { get; init; }

    [SetsRequiredMembers]
    public CustomBlock(string customType, string? source = null, IReadOnlyDictionary<string, string>? attributes = null)
        : base(BlockType.Custom)
    {
        CustomType = customType;
        Source = source;
        Attributes = attributes;
    }
}
