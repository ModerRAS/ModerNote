using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Modernote.Client;
using Modernote.Protocol;
using Modernote.Service;
using Xunit;

namespace Modernote.Core.Tests;

public class ServiceIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ModernoteService _service;
    private readonly ModernoteClient _client;

    public ServiceIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "modernote-int-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _service = new ModernoteService();
        _client = new ModernoteClient(new EmbeddedTransport(_service));
    }

    public void Dispose()
    {
        _service.Close();
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public async Task EndToEnd_OpenScanCreateList()
    {
        await _client.OpenVaultAsync(_tempDir);
        var scanCount = await _client.ScanAsync();
        Assert.Equal(0, scanCount);

        var note = await _client.CreateNoteAsync("My First Note", null);
        Assert.NotEqual(Guid.Empty, note.Id);
        Assert.Equal(ObjectKind.XmlNote, note.Kind);

        var objects = await _client.ListObjectsAsync();
        Assert.Single(objects);
    }

    [Fact]
    public async Task EndToEnd_CreateSaveLoad()
    {
        await _client.OpenVaultAsync(_tempDir);
        var note = await _client.CreateNoteAsync("Test", null);
        var newXml = "<document version=\"1\"><h1>Updated</h1><p>New body</p></document>";
        await _client.SaveNoteAsync(note.Id, newXml);
        var loaded = await _client.LoadNoteAsync(note.Id);
        Assert.Contains("Updated", loaded);
        Assert.Contains("New body", loaded);
    }

    [Fact]
    public async Task EndToEnd_Import()
    {
        await _client.OpenVaultAsync(_tempDir);
        var src = Path.Combine(_tempDir, "photo.jpg");
        File.WriteAllText(src, "fake-jpg");
        var obj = await _client.ImportFileAsync(src, "images");
        Assert.StartsWith("assets/images/", obj.LogicalPath);
    }

    [Fact]
    public async Task EndToEnd_Search()
    {
        await _client.OpenVaultAsync(_tempDir);
        var note = await _client.CreateNoteAsync("Samsung SSD Review", null);
        // Refresh text index so search works
        _service.Scan();
        var results = await _client.SearchAsync("Samsung", 10);
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task EndToEnd_Resolve()
    {
        await _client.OpenVaultAsync(_tempDir);
        var note = await _client.CreateNoteAsync("Resolvable", null);
        var found = await _client.ResolveObjectAsync(note.Id);
        Assert.NotNull(found);
        Assert.Equal(note.Id, found!.Id);
    }

    [Fact]
    public async Task EndToEnd_ResolveMissing_ReturnsNull()
    {
        await _client.OpenVaultAsync(_tempDir);
        var found = await _client.ResolveObjectAsync(Guid.NewGuid());
        Assert.Null(found);
    }

    [Fact]
    public async Task EndToEnd_OpenReopensExistingVault()
    {
        // First session: create a note
        await _client.OpenVaultAsync(_tempDir);
        var note = await _client.CreateNoteAsync("Persistent", null);

        // Close, re-open
        _service.Close();
        var svc2 = new ModernoteService();
        var cli2 = new ModernoteClient(new EmbeddedTransport(svc2));
        await cli2.OpenVaultAsync(_tempDir);
        var found = await cli2.ResolveObjectAsync(note.Id);
        Assert.NotNull(found);
        svc2.Close();
    }
}
