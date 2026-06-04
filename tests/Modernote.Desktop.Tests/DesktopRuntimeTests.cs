using System;
using System.IO;
using System.Threading.Tasks;
using Modernote.Client;
using Modernote.Protocol;
using Modernote.Service;
using Xunit;

namespace Modernote.Desktop.Tests;

public class DesktopRuntimeTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ModernoteService _service;
    private readonly ModernoteClient _client;
    private readonly DesktopRuntime _runtime;

    public DesktopRuntimeTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "modernote-dt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _service = new ModernoteService();
        _client = new ModernoteClient(new EmbeddedTransport(_service));
        _runtime = new DesktopRuntime(_client);
    }

    public void Dispose()
    {
        _service.Close();
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public async Task OpenVaultAsync_SetsVaultRoot()
    {
        await _runtime.OpenVaultAsync(_tempDir);

        Assert.Equal(_tempDir, _runtime.State.VaultRoot);
        // ScanAsync is called internally, so status shows scan results
        Assert.Contains("0 objects", _runtime.State.StatusMessage);
    }

    [Fact]
    public async Task ScanAsync_Initially_ReturnsZero()
    {
        await _runtime.OpenVaultAsync(_tempDir);
        await _runtime.ScanAsync();

        Assert.Empty(_runtime.State.Objects);
        Assert.Contains("0 objects", _runtime.State.StatusMessage);
    }

    [Fact]
    public async Task ScanAsync_AfterCreatingNote_HasOneObject()
    {
        await _runtime.OpenVaultAsync(_tempDir);
        var note = await _client.CreateNoteAsync("Test Note", null);

        await _runtime.ScanAsync();

        var obj = Assert.Single(_runtime.State.Objects);
        Assert.Equal(note.Id, obj.Id);
        Assert.Equal(ObjectKind.XmlNote, obj.Kind);
    }

    [Fact]
    public async Task CreateNoteAsync_AddsObjectAndSelectsIt()
    {
        await _runtime.OpenVaultAsync(_tempDir);

        await _runtime.CreateNoteAsync("My New Note");

        var obj = Assert.Single(_runtime.State.Objects);
        Assert.Contains("My New Note", obj.DisplayName);
        Assert.NotNull(_runtime.State.SelectedObject);
        Assert.Equal(obj.Id, _runtime.State.SelectedObject!.Id);
    }

    [Fact]
    public async Task SelectAsync_XmlNote_LoadsEditorXml()
    {
        await _runtime.OpenVaultAsync(_tempDir);
        var note = await _client.CreateNoteAsync("Selectable", null);

        await _runtime.SelectAsync(note);

        Assert.Contains("<document", _runtime.State.EditorXml);
    }

    [Fact]
    public async Task SelectAsync_NonNote_ClearsEditorXml()
    {
        await _runtime.OpenVaultAsync(_tempDir);
        // Create an asset file (non-note kind)
        var src = Path.Combine(_tempDir, "test.txt");
        await File.WriteAllTextAsync(src, "plain text");
        var asset = await _client.ImportFileAsync(src, "files");

        await _runtime.SelectAsync(asset);

        Assert.Equal(string.Empty, _runtime.State.EditorXml);
    }

    [Fact]
    public async Task State_Initially_HasDefaultValues()
    {
        Assert.Equal(string.Empty, _runtime.State.VaultRoot);
        Assert.Equal("No vault open", _runtime.State.StatusMessage);
        Assert.Null(_runtime.State.SelectedObject);
        Assert.Equal(string.Empty, _runtime.State.EditorXml);
        Assert.False(_runtime.State.IsDirty);
        Assert.Empty(_runtime.State.Objects);
        Assert.Empty(_runtime.State.SearchResults);
    }
}
