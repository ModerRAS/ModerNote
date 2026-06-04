using Modernote.Core.Model;

namespace Modernote.Desktop.Editor;

public static class BlockViewModelFactory
{
    public static BlockViewModel Create(Block block) => block switch
    {
        ParagraphBlock pb => new TextBlockViewModel(pb),
        CodeBlock cb => new CodeBlockViewModel(cb),
        _ => new GenericBlockViewModel(block)
    };
}
