using System;
using System.IO;
using System.Threading.Tasks;
using Modernote.Client;
using Modernote.Service;
using Modernote.Protocol;
using Xunit;

namespace Modernote.Integration.Tests;

public class VaultFlowIntegrationTests : IDisposable
{
    private readonly string _vaultDir;
    private readonly string _srcDir;
    private readonly ModernoteService _service;
    private readonly ModernoteClient _client;

    public VaultFlowIntegrationTests()
    {
        _vaultDir = Path.Combine(Path.GetTempPath(), "modernote-int-" + Guid.NewGuid().ToString("N"));
        _srcDir = Path.Combine(Path.GetTempPath(), "modernote-src-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_vaultDir);
        Directory.CreateDirectory(_srcDir);
        _service = new ModernoteService();
        _client = new ModernoteClient(new EmbeddedTransport(_service));
    }

    public void Dispose()
    {
        _service.Close();
        try { Directory.Delete(_vaultDir, recursive: true); } catch { }
        try { Directory.Delete(_srcDir, recursive: true); } catch { }
    }

    [Fact]
    public async Task FullFlow_OpenCreateSaveLoad_Works()
    {
        // Open vault
        var root = await _client.OpenVaultAsync(_vaultDir);
        Assert.Equal(_vaultDir, root);

        // Create note
        var note = await _client.CreateNoteAsync("Integration Test", null);
        Assert.Equal("Integration Test.xml", note.DisplayName);
        Assert.Equal(ObjectKind.XmlNote, note.Kind);

        // Save updated content
        var newXml = "<document version=\"1\"><h1>Updated</h1><p>New</p></document>";
        await _client.SaveNoteAsync(note.Id, newXml);

        // Load and verify
        var loaded = await _client.LoadNoteAsync(note.Id);
        Assert.Contains("Updated", loaded);

        // List and verify
        var all = await _client.ListObjectsAsync();
        Assert.Contains(all, o => o.Id == note.Id);
    }

    [Fact]
    public async Task Scan_AfterAddingFiles_IndexesThem()
    {
        await _client.OpenVaultAsync(_vaultDir);
        File.WriteAllText(Path.Combine(_vaultDir, "notes", "test.xml"), "<document/>");
        File.WriteAllText(Path.Combine(_vaultDir, "assets", "test.png"), "fake-png");
        var count = await _client.ScanAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Import_CopiesFileToAssets()
    {
        await _client.OpenVaultAsync(_vaultDir);
        var src = Path.Combine(_srcDir, "photo.jpg");
        File.WriteAllText(src, "fake-jpg");
        var obj = await _client.ImportFileAsync(src, "photos");
        Assert.Equal(ObjectKind.Image, obj.Kind);
        Assert.Contains("photos", obj.LogicalPath);
    }

    [Fact]
    public async Task Search_ReturnsResults_WhenNoteCreated()
    {
        await _client.OpenVaultAsync(_vaultDir);
        var note = await _client.CreateNoteAsync("Searchable Title", null);
        // Notes are now automatically indexed in FTS5 upon creation via RefreshTextIndex.
        var results = await _client.SearchAsync("Searchable", 10);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Object.Id == note.Id);
    }

    [Fact]
    public async Task ResolveObject_AfterCreate_ReturnsObject()
    {
        await _client.OpenVaultAsync(_vaultDir);
        var note = await _client.CreateNoteAsync("Test", null);
        var resolved = await _client.ResolveObjectAsync(note.Id);
        Assert.NotNull(resolved);
        Assert.Equal(note.Id, resolved!.Id);
    }
}
