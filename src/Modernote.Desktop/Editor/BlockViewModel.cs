using CommunityToolkit.Mvvm.ComponentModel;
using Modernote.Core.Model;

namespace Modernote.Desktop.Editor;

public abstract partial class BlockViewModel : ObservableObject
{
    [ObservableProperty] private Block block = null!;
    [ObservableProperty] private bool isFocused;

    public BlockViewModel(Block block) => Block = block;

    public abstract string DisplayText { get; }
}
