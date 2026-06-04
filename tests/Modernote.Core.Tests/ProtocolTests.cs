using System.Text.Json;
using Modernote.Protocol;

namespace Modernote.Core.Tests;

public class ProtocolTests
{
    [Fact]
    public void ObjectDto_RoundTrip_PreservesAllFields()
    {
        var id = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow;
        var updated = created.AddMinutes(5);
        var dto = new ObjectDto(
            id, ObjectKind.XmlNote, "notes/test.xml", "test.xml",
            "abc123", "text/xml", 1024, created, updated);

        var json = JsonSerializer.Serialize(dto);
        var roundtripped = JsonSerializer.Deserialize<ObjectDto>(json);

        Assert.NotNull(roundtripped);
        Assert.Equal(id, roundtripped.Id);
        Assert.Equal(ObjectKind.XmlNote, roundtripped.Kind);
        Assert.Equal("notes/test.xml", roundtripped.LogicalPath);
        Assert.Equal("test.xml", roundtripped.DisplayName);
        Assert.Equal("abc123", roundtripped.ContentHash);
        Assert.Equal("text/xml", roundtripped.Mime);
        Assert.Equal(1024, roundtripped.SizeBytes);
        Assert.Equal(created, roundtripped.CreatedAt);
        Assert.Equal(updated, roundtripped.UpdatedAt);
    }

    [Fact]
    public void ApiRequest_OpenVault_RoundTrip()
    {
        var req = new ApiRequest.OpenVault(@"C:\MyVault");
        var json = JsonSerializer.Serialize<ApiRequest>(req);
        var rt = JsonSerializer.Deserialize<ApiRequest>(json);
        var typed = Assert.IsType<ApiRequest.OpenVault>(rt);
        Assert.Equal(@"C:\MyVault", typed.Root);
    }

    [Fact]
    public void ApiResponse_AllVariants_Constructible()
    {
        _ = new ApiResponse.VaultOpened("path");
        _ = new ApiResponse.ScanCompleted(42);
        _ = new ApiResponse.SearchResults(Array.Empty<SearchResultDto>());
        Assert.True(true);
    }

    [Fact]
    public void ClientContext_LocalDesktop_DefaultsToDesktopKind()
    {
        var ctx = ClientContext.LocalDesktop();
        Assert.Equal("local-desktop", ctx.ClientId);
        Assert.Equal(ClientKind.Desktop, ctx.ClientKind);
    }
}
