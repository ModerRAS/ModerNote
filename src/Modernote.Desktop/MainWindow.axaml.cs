using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Modernote.Client;
using Modernote.Protocol;

namespace Modernote.Desktop;

public partial class MainWindow : Window
{
    private ModernoteClient? _client;
    private DesktopRuntime? _runtime;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void Initialize(ModernoteClient client, DesktopRuntime runtime)
    {
        _client = client;
        _runtime = runtime;
        ObjectList.ItemsSource = runtime.State.Objects;
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
