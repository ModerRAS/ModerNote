using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Modernote.Client;
using Modernote.Desktop.Editor;
using Modernote.Core.Model;
using Modernote.Protocol;

namespace Modernote.Desktop;

public partial class MainWindow : Window
{
    private ModernoteClient? _client;
    private DesktopRuntime? _runtime;
    private EditorHost? _host;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void Initialize(ModernoteClient client, DesktopRuntime runtime)
    {
        _client = client;
        _runtime = runtime;

        ObjectList.ItemsSource = runtime.State.Objects;
        PageTitle.Text = "No note selected";

        // Initialize EditorHost with the editor StackPanel
        _host = new EditorHost(EditorPanel);
        runtime.State.Host = _host;

        // Set sidebar DataContext to enable bindings (StatusText, MetaPanel)
        SidebarPanel.DataContext = runtime.State;

        // Wire selection change
        ObjectList.SelectionChanged += OnObjectSelected;

        // Observe state changes for status bar
        runtime.State.PropertyChanged += OnStatePropertyChanged;
        UpdateStatusBar();

        // Wire search
        SearchBox.TextChanged += OnSearchTextChanged;
        SearchResultsList.SelectionChanged += OnSearchResultSelected;
    }

    private async void OnObjectSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_runtime == null) return;
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is ObjectDto obj)
        {
            await _runtime.SelectAsync(obj);
            PageTitle.Text = obj.DisplayName;
            SaveButton.IsEnabled = true;
            _runtime.State.StatusMessage = "Loaded";
        }
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e) => _ = SaveCurrentNoteAsync();

    private void OnAddBlockClick(object? sender, RoutedEventArgs e)
    {
        if (_host == null) return;
        var menu = BlockMenu.CreateAddBlockMenu(blockType =>
        {
            var block = BlockMenu.CreateBlock(blockType);
            _host.AddBlock(block);
            _runtime!.State.IsDirty = true;
        });
        var button = sender as Control;
        if (button != null) menu.ShowAt(button);
    }

    private async Task SaveCurrentNoteAsync()
    {
        if (_runtime == null) return;
        await _runtime.SaveCurrentNoteAsync();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.S && e.KeyModifiers == KeyModifiers.Control)
        {
            _ = SaveCurrentNoteAsync();
            e.Handled = true;
        }
        else if (e.Key == Key.Z && e.KeyModifiers == KeyModifiers.Control)
        {
            OnUndoClick(null, null!);
            e.Handled = true;
        }
        else if (e.Key == Key.Y && e.KeyModifiers == KeyModifiers.Control)
        {
            OnRedoClick(null, null!);
            e.Handled = true;
        }
        else if (e.Key == Key.Up && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
        {
            if (_host != null && _host.CurrentIndex >= 0)
            {
                _host.MoveBlockUp(_host.CurrentIndex);
                _runtime!.State.IsDirty = true;
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Down && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
        {
            if (_host != null && _host.CurrentIndex >= 0)
            {
                _host.MoveBlockDown(_host.CurrentIndex);
                _runtime!.State.IsDirty = true;
            }
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        if (_runtime == null) return;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import File",
            AllowMultiple = true
        });
        foreach (var file in files)
        {
            try
            {
                await _client!.ImportFileAsync(file.Path.LocalPath, "imports");
                await _runtime.ScanAsync();
            }
            catch (Exception ex)
            {
                _runtime.State.StatusMessage = $"Import failed: {ex.Message}";
            }
        }
    }

    private async void OnNewNoteClick(object? sender, RoutedEventArgs e)
    {
        if (_runtime == null) return;
        await _runtime.CreateNoteAsync("Untitled");
    }

    private async void OnScanClick(object? sender, RoutedEventArgs e)
    {
        if (_runtime == null) return;
        await _runtime.ScanAsync();
        UpdateStatusBar();
    }

    private async void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_runtime == null || SearchBox == null) return;
        var query = SearchBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(query))
        {
            ObjectList.ItemsSource = _runtime.State.Objects;
            SearchResultsPanel.IsVisible = false;
            _runtime.State.SearchResults.Clear();
            UpdateStatusBar();
            return;
        }

        // Filter sidebar ObjectList by DisplayName (real-time)
        var filtered = _runtime.State.Objects
            .Where(o => o.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
        ObjectList.ItemsSource = filtered;

        // Full-text content search for SearchResultsPanel
        await _runtime.SearchAsync(query);
        SearchResultsPanel.IsVisible = _runtime.State.SearchResults.Count > 0;
    }

    private async void OnSearchResultSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_runtime == null || e.AddedItems.Count == 0) return;
        if (e.AddedItems[0] is SearchResultDto result)
        {
            await _runtime.SelectAsync(result.Object);
            PageTitle.Text = result.Object.DisplayName;
            SaveButton.IsEnabled = true;
            _runtime.State.StatusMessage = "Loaded";
            SearchBox.Text = string.Empty;
            SearchResultsPanel.IsVisible = false;
        }
    }

    private void OnStatePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DesktopState.StatusMessage):
                StatusBarInfo.Text = _runtime?.State.StatusMessage ?? "Ready";
                break;
            case nameof(DesktopState.IsDirty):
            case nameof(DesktopState.SelectedObject):
                UpdateStatusBar();
                break;
        }
    }

    private void UpdateStatusBar()
    {
        if (_runtime == null) return;
        var state = _runtime.State;

        StatusObjectCount.Text = $"{state.Objects.Count} objects";
        StatusSelection.Text = state.SelectedObject != null
            ? $"Selected: {state.SelectedObject.DisplayName}"
            : "No selection";
        StatusDirty.Text = state.IsDirty ? "\u25CF Unsaved" : "";
        StatusDirty.Foreground = state.IsDirty
            ? new SolidColorBrush(Color.FromRgb(0xCC, 0x88, 0x00))
            : Brushes.Transparent;
    }

    // --- Undo / Redo ---

    private void OnUndoClick(object? sender, RoutedEventArgs e)
    {
        if (_runtime == null || _host == null) return;
        if (_runtime.State.UndoStack.Count == 0) return;
        _runtime.State.RedoStack.Push(_host.SerializeDocument());
        var prev = _runtime.State.UndoStack.Pop();
        LoadXmlIntoEditor(prev);
        _runtime.State.StatusMessage = "Undo";
    }

    private void OnRedoClick(object? sender, RoutedEventArgs e)
    {
        if (_runtime == null || _host == null) return;
        if (_runtime.State.RedoStack.Count == 0) return;
        _runtime.State.UndoStack.Push(_host.SerializeDocument());
        var next = _runtime.State.RedoStack.Pop();
        LoadXmlIntoEditor(next);
        _runtime.State.StatusMessage = "Redo";
    }

    private void LoadXmlIntoEditor(string xml)
    {
        if (_host == null) return;
        _host.LoadXml(xml);
    }
}
