using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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

        // Wire selection change
        ObjectList.SelectionChanged += OnObjectSelected;
    }

    private async void OnObjectSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_runtime == null) return;
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is ObjectDto obj)
        {
            await _runtime.SelectAsync(obj);
            PageTitle.Text = obj.DisplayName;
            SaveButton.IsEnabled = true;
            StatusText.Text = "Loaded";
        }
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        await SaveCurrentNoteAsync();
    }

    private async void OnAddBlockClick(object? sender, RoutedEventArgs e)
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
        StatusText.Text = "Saved";
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.S && e.KeyModifiers == KeyModifiers.Control)
        {
            _ = SaveCurrentNoteAsync();
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
                StatusText.Text = $"Import failed: {ex.Message}";
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
    }
}
