using CommunityToolkit.Mvvm.ComponentModel;
using Modernote.Core.Model;

namespace Modernote.Desktop.Editor;

public partial class TextBlockViewModel : BlockViewModel
{
    [ObservableProperty] private string text = string.Empty;

    public TextBlockViewModel(ParagraphBlock block) : base(block)
    {
        Text = block.Text;
    }

    public override string DisplayText => Text;
}
