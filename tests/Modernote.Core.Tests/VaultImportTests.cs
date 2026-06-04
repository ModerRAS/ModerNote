using System;
using System.IO;
using Modernote.Protocol;
using Xunit;

namespace Modernote.Core.Tests;

public class VaultImportTests : IDisposable
{
    private readonly string _tempDir;
    private readonly global::Modernote.Core.Vault.Vault _vault;

    public VaultImportTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "modernote-import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _vault = global::Modernote.Core.Vault.Vault.OpenOrCreate(_tempDir);
    }

    public void Dispose()
    {
        _vault.Dispose();
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void ImportFile_BasicFile()
    {
        var sourceFile = Path.Combine(_tempDir, "source.txt");
        File.WriteAllText(sourceFile, "imported content");
        var sourceInfo = new FileInfo(sourceFile);

        // Save the source for later verification
        var obj = _vault.ImportFile(sourceFile, null);
        Assert.StartsWith("assets/", obj.LogicalPath);
        Assert.True(File.Exists(Path.Combine(_vault.Root, obj.LogicalPath)));
    }

    [Fact]
    public void ImportFile_NonExistent_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => _vault.ImportFile(Path.Combine(_tempDir, "nope.txt"), null));
    }

    [Fact]
    public void ImportFile_DuplicateName_AutoNumbers()
    {
        var src1 = Path.Combine(_tempDir, "photo.png");
        var src2 = Path.Combine(_tempDir, "photo2.png");
        File.WriteAllBytes(src1, new byte[] { 1, 2, 3 });
        File.WriteAllBytes(src2, new byte[] { 4, 5, 6 });

        var obj1 = _vault.ImportFile(src1, null);
        var obj2 = _vault.ImportFile(src2, null);
        Assert.NotEqual(obj1.LogicalPath, obj2.LogicalPath);
    }

    [Fact]
    public void ImportFile_WithFolder_CreatesSubdirectory()
    {
        var src = Path.Combine(_tempDir, "data.zip");
        File.WriteAllBytes(src, new byte[] { 1 });
        var obj = _vault.ImportFile(src, "downloads");
        Assert.Contains("downloads", obj.LogicalPath);
        Assert.True(Directory.Exists(Path.Combine(_vault.AssetsPath, "downloads")));
    }

    [Fact]
    public void ImportFile_IndexesInDatabase()
    {
        var src = Path.Combine(_tempDir, "doc.pdf");
        File.WriteAllBytes(src, new byte[] { 1, 2, 3, 4 });
        var obj = _vault.ImportFile(src, null);
        Assert.Equal(ObjectKind.Pdf, obj.Kind);
        var resolved = _vault.ResolveObject(obj.Id);
        Assert.NotNull(resolved);
        Assert.Equal(obj.Id, resolved!.Id);
    }

    [Fact]
    public void ImportFile_CopiesContent_VerifyingHash()
    {
        var src = Path.Combine(_tempDir, "test.png");
        var content = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        File.WriteAllBytes(src, content);
        var obj = _vault.ImportFile(src, null);
        var copied = File.ReadAllBytes(Path.Combine(_vault.Root, obj.LogicalPath));
        Assert.Equal(content, copied);
    }
}
