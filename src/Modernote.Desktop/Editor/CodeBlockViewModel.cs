using CommunityToolkit.Mvvm.ComponentModel;
using Modernote.Core.Model;

namespace Modernote.Desktop.Editor;

public partial class CodeBlockViewModel : BlockViewModel
{
    [ObservableProperty] private string code = string.Empty;
    [ObservableProperty] private string language = string.Empty;

    public CodeBlockViewModel(CodeBlock block) : base(block)
    {
        Code = block.Code;
        Language = block.Language ?? string.Empty;
    }

    public override string DisplayText => $"```{Language}\n{Code}\n```";
}
