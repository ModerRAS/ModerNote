using System;
using System.IO;
using System.Linq;
using Modernote.Core.Exceptions;
using Modernote.Core.Vault;
using Modernote.Protocol;
using Xunit;

namespace Modernote.Core.Tests;

public class VaultScanAndCrudTests : IDisposable
{
    private readonly string _tempDir;
    private readonly global::Modernote.Core.Vault.Vault _vault;

    public VaultScanAndCrudTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "modernote-crud-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _vault = global::Modernote.Core.Vault.Vault.OpenOrCreate(_tempDir);
    }

    public void Dispose()
    {
        _vault.Dispose();
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    // === T12 Scan Tests ===

    [Fact]
    public void Scan_EmptyVault_ReturnsZero()
    {
        var result = _vault.Scan();
        Assert.Equal(0, result.ObjectsIndexed);
    }

    [Fact]
    public void Scan_WithFiles_IndexesAll()
    {
        File.WriteAllText(Path.Combine(_vault.NotesPath, "note1.xml"), "<document/>");
        File.WriteAllText(Path.Combine(_vault.NotesPath, "note2.xml"), "<document/>");
        File.WriteAllText(Path.Combine(_vault.AssetsPath, "img.png"), "fake");
        var result = _vault.Scan();
        Assert.Equal(3, result.ObjectsIndexed);
    }

    [Fact]
    public void Scan_SameFileTwice_NoDuplicate()
    {
        File.WriteAllText(Path.Combine(_vault.NotesPath, "note.xml"), "<document/>");
        _vault.Scan();
        var second = _vault.Scan();
        Assert.Equal(1, second.ObjectsIndexed);
    }

    [Fact]
    public void Scan_FileInSubdirectory_IndexedWithRelativePath()
    {
        var sub = Path.Combine(_vault.NotesPath, "subdir");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "deep.xml"), "<document/>");
        _vault.Scan();
        var objs = _vault.ListObjects();
        Assert.Single(objs);
        Assert.Contains("subdir", objs[0].LogicalPath);
    }

    [Fact]
    public void Scan_FileHash_IsPersisted()
    {
        var content = "test content " + Guid.NewGuid();
        File.WriteAllText(Path.Combine(_vault.AssetsPath, "test.txt"), content);
        _vault.Scan();
        var obj = _vault.ListObjects()[0];
        Assert.Equal(64, obj.ContentHash.Length); // SHA-256 hex
    }

    // === T13 CRUD Tests ===

    [Fact]
    public void CreateXmlNote_CreatesFileAndIndexes()
    {
        var note = _vault.CreateXmlNote("My First Note", null);
        Assert.Equal("My First Note.xml", note.DisplayName);
        Assert.Equal(ObjectKind.XmlNote, note.Kind);
        Assert.True(File.Exists(Path.Combine(_vault.NotesPath, "My First Note.xml")));
        Assert.NotNull(_vault.ResolveObject(note.Id));
    }

    [Fact]
    public void CreateXmlNote_ReplacesInvalidChars()
    {
        var note = _vault.CreateXmlNote("My/Bad:Note*Name", null);
        var fileName = new DirectoryInfo(_vault.NotesPath).GetFiles()[0].Name;
        Assert.DoesNotContain("/", fileName);
        Assert.DoesNotContain(":", fileName);
        Assert.DoesNotContain("*", fileName);
    }

    [Fact]
    public void CreateXmlNote_DuplicateTitle_AutoNumbers()
    {
        var first = _vault.CreateXmlNote("Duplicate", null);
        var second = _vault.CreateXmlNote("Duplicate", null);
        Assert.NotEqual(first.LogicalPath, second.LogicalPath);
    }

    [Fact]
    public void CreateXmlNote_WithFolder_CreatesSubdirectory()
    {
        var note = _vault.CreateXmlNote("Sub Note", "my-folder");
        Assert.Contains("my-folder", note.LogicalPath);
    }

    [Fact]
    public void SaveAndLoadNoteXml_RoundTrips()
    {
        var note = _vault.CreateXmlNote("Test", null);
        var newXml = "<document version=\"1\"><h1>Updated</h1><p>New content</p></document>";
        _vault.SaveNoteXml(note.Id, newXml);
        var (_, loadedXml) = _vault.LoadNoteXml(note.Id);
        Assert.Equal(newXml, loadedXml);
    }

    [Fact]
    public void LoadNoteXml_NonExistent_Throws()
    {
        Assert.Throws<ObjectNotFoundException>(() => _vault.LoadNoteXml(Guid.NewGuid()));
    }

    [Fact]
    public void ListObjects_ReturnsAllCreated()
    {
        _vault.CreateXmlNote("Note 1", null);
        _vault.CreateXmlNote("Note 2", null);
        _vault.CreateXmlNote("Note 3", null);
        var all = _vault.ListObjects();
        Assert.True(all.Count >= 3);
    }

    [Fact]
    public void ResolveObject_ExistingId_ReturnsObject()
    {
        var note = _vault.CreateXmlNote("Find Me", null);
        var found = _vault.ResolveObject(note.Id);
        Assert.NotNull(found);
        Assert.Equal(note.Id, found!.Id);
    }

    [Fact]
    public void ResolveObject_NonExistent_ReturnsNull()
    {
        Assert.Null(_vault.ResolveObject(Guid.NewGuid()));
    }
}
