using System;
using System.IO;
using System.Linq;
using Modernote.Core.Vault;
using Xunit;

namespace Modernote.Core.Tests;

public class TextIndexRefreshTests : IDisposable
{
    private readonly string _dir;
    private readonly global::Modernote.Core.Vault.Vault _vault;

    public TextIndexRefreshTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "modernote-ti-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _vault = global::Modernote.Core.Vault.Vault.OpenOrCreate(_dir);
    }

    public void Dispose()
    {
        _vault.Dispose();
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    [Fact]
    public void RefreshTextIndex_PopulatesObjectTextAndFts()
    {
        var note = _vault.CreateXmlNote("Samsung Test", null);
        _vault.RefreshTextIndex(note.Id);

        // object_text should have entry
        using var cmd1 = _vault.Connection.CreateCommand();
        cmd1.CommandText = "SELECT title, body FROM object_text WHERE object_id = $id";
        cmd1.Parameters.AddWithValue("$id", note.Id.ToString());
        using var reader = cmd1.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Contains("Samsung", reader.GetString(0));

        // object_fts should have entry
        using var cmd2 = _vault.Connection.CreateCommand();
        cmd2.CommandText = "SELECT COUNT(*) FROM object_fts WHERE object_id = $id";
        cmd2.Parameters.AddWithValue("$id", note.Id.ToString());
        var count = (long)(cmd2.ExecuteScalar() ?? 0L);
        Assert.Equal(1, count);
    }

    [Fact]
    public void RefreshTextIndex_AfterUpdate_ReplacesOld()
    {
        var note = _vault.CreateXmlNote("First", null);
        _vault.RefreshTextIndex(note.Id);
        // Now update content and refresh again
        var newXml = "<document version=\"1\"><h1>Updated</h1><p>New content</p></document>";
        _vault.SaveNoteXml(note.Id, newXml);
        _vault.RefreshTextIndex(note.Id);

        // Should now have "Updated" not "First"
        using var cmd = _vault.Connection.CreateCommand();
        cmd.CommandText = "SELECT title FROM object_text WHERE object_id = $id";
        cmd.Parameters.AddWithValue("$id", note.Id.ToString());
        using var reader = cmd.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal("Updated", reader.GetString(0));
    }
}
