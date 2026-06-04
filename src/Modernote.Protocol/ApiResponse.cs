using System.Text.Json.Serialization;

namespace Modernote.Protocol;

/// <summary>Response types from service to client. Each response pairs with a request type.</summary>
[JsonDerivedType(typeof(ApiResponse.VaultOpened), "vaultOpened")]
[JsonDerivedType(typeof(ApiResponse.ScanCompleted), "scanCompleted")]
[JsonDerivedType(typeof(ApiResponse.XmlNoteCreated), "xmlNoteCreated")]
[JsonDerivedType(typeof(ApiResponse.NoteSaved), "noteSaved")]
[JsonDerivedType(typeof(ApiResponse.NoteXmlLoaded), "noteXmlLoaded")]
[JsonDerivedType(typeof(ApiResponse.FileImported), "fileImported")]
[JsonDerivedType(typeof(ApiResponse.SearchResults), "searchResults")]
[JsonDerivedType(typeof(ApiResponse.ObjectResolved), "objectResolved")]
[JsonDerivedType(typeof(ApiResponse.ObjectsListed), "objectsListed")]
public abstract record ApiResponse
{
    public sealed record VaultOpened(string Root) : ApiResponse;
    public sealed record ScanCompleted(int ObjectsIndexed) : ApiResponse;
    public sealed record XmlNoteCreated(ObjectDto Object, string Xml) : ApiResponse;
    public sealed record NoteSaved(ObjectDto Object) : ApiResponse;
    public sealed record NoteXmlLoaded(ObjectDto Object, string Xml) : ApiResponse;
    public sealed record FileImported(ObjectDto Object) : ApiResponse;
    public sealed record SearchResults(IReadOnlyList<SearchResultDto> Results) : ApiResponse;
    public sealed record ObjectResolved(ObjectDto? Object) : ApiResponse;
    public sealed record ObjectsListed(IReadOnlyList<ObjectDto> Objects) : ApiResponse;
}
