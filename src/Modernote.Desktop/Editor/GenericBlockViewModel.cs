using Modernote.Core.Model;

namespace Modernote.Desktop.Editor;

public class GenericBlockViewModel : BlockViewModel
{
    public GenericBlockViewModel(Block block) : base(block) { }
    public override string DisplayText => Block.ToString() ?? string.Empty;
}
