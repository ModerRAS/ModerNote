using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Modernote.Desktop.Editor;
using Modernote.Protocol;

namespace Modernote.Desktop;

public partial class DesktopState : ObservableObject
{
    [ObservableProperty] private string vaultRoot = string.Empty;
    [ObservableProperty] private string searchQuery = string.Empty;
    [ObservableProperty] private string statusMessage = "No vault open";
    [ObservableProperty] private ObjectDto? selectedObject;
    [ObservableProperty] private string editorXml = string.Empty;
    [ObservableProperty] private bool isDirty;

    public ObservableCollection<ObjectDto> Objects { get; } = new();
    public ObservableCollection<SearchResultDto> SearchResults { get; } = new();

    /// <summary>The active block editor host. Set by MainWindow on initialization.</summary>
    public EditorHost? Host { get; set; }
}
