using System;
using System.IO;
using System.Linq;
using Modernote.Core.Vault;
using Xunit;

namespace Modernote.Core.Tests;

public class TagsAndLinksTests : IDisposable
{
    private readonly string _dir;
    private readonly global::Modernote.Core.Vault.Vault _vault;

    public TagsAndLinksTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "modernote-tags-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _vault = global::Modernote.Core.Vault.Vault.OpenOrCreate(_dir);
    }

    public void Dispose()
    {
        _vault.Dispose();
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    [Fact]
    public void AddTag_CreatesAndLinks()
    {
        var note = _vault.CreateXmlNote("Test", null);
        var tag = _vault.AddTag(note.Id, "important");
        Assert.Equal("important", tag.Name);
        Assert.NotEqual(Guid.Empty, tag.Id);
        var tags = _vault.GetTags(note.Id);
        Assert.Single(tags);
        Assert.Equal("important", tags[0].Name);
    }

    [Fact]
    public void AddTag_SameName_ReusesTag()
    {
        var note1 = _vault.CreateXmlNote("Note1", null);
        var note2 = _vault.CreateXmlNote("Note2", null);
        var tag1 = _vault.AddTag(note1.Id, "shared");
        var tag2 = _vault.AddTag(note2.Id, "shared");
        Assert.Equal(tag1.Id, tag2.Id); // same tag id
    }

    [Fact]
    public void RemoveTag_Unlinks()
    {
        var note = _vault.CreateXmlNote("Test", null);
        _vault.AddTag(note.Id, "temp");
        _vault.RemoveTag(note.Id, "temp");
        Assert.Empty(_vault.GetTags(note.Id));
    }

    [Fact]
    public void AddLink_RecordsCorrectly()
    {
        var from = _vault.CreateXmlNote("From", null);
        var to = _vault.CreateXmlNote("To", null);
        var link = _vault.AddLink(from.Id, to.Id, "wikilink", to.LogicalPath);
        Assert.Equal(from.Id, link.FromObjectId);
        Assert.Equal(to.Id, link.ToObjectId);
        var links = _vault.GetLinks(from.Id);
        Assert.Single(links);
    }

    [Fact]
    public void AddLink_ExternalLink_Allowed()
    {
        var from = _vault.CreateXmlNote("From", null);
        var link = _vault.AddLink(from.Id, null, "external", "https://example.com");
        Assert.Null(link.ToObjectId);
        Assert.Equal("https://example.com", link.Target);
    }
}
