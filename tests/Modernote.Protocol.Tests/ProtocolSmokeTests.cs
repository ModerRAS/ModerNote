using Modernote.Protocol;

namespace Modernote.Protocol.Tests;

public class ProtocolSmokeTests
{
    [Fact]
    public void ObjectKind_HasNineValues()
    {
        Assert.Equal(9, Enum.GetValues<ObjectKind>().Length);
    }

    [Fact]
    public void ObjectKind_RoundTrips_Json()
    {
        var kind = ObjectKind.XmlNote;
        var json = System.Text.Json.JsonSerializer.Serialize(kind);
        var parsed = System.Text.Json.JsonSerializer.Deserialize<ObjectKind>(json);
        Assert.Equal(kind, parsed);
    }

    [Fact]
    public void ObjectDto_RecordEquality()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var a = new ObjectDto(id, ObjectKind.Pdf, "a.pdf", "a.pdf", "hash", "application/pdf", 100, now, now);
        var b = new ObjectDto(id, ObjectKind.Pdf, "a.pdf", "a.pdf", "hash", "application/pdf", 100, now, now);
        Assert.Equal(a, b);
    }
}
