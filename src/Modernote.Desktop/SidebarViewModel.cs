using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Modernote.Protocol;

namespace Modernote.Desktop;

public partial class SidebarViewModel : ObservableObject
{
    [ObservableProperty] private string searchText = string.Empty;
    public ObservableCollection<ObjectDto> Items { get; } = new();
}
