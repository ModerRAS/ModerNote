using System.Text.Json.Serialization;

namespace Modernote.Protocol;

/// <summary>Request types from client to service.</summary>
[JsonDerivedType(typeof(ApiRequest.OpenVault), "openVault")]
[JsonDerivedType(typeof(ApiRequest.Scan), "scan")]
[JsonDerivedType(typeof(ApiRequest.CreateXmlNote), "createXmlNote")]
[JsonDerivedType(typeof(ApiRequest.SaveNoteXml), "saveNoteXml")]
[JsonDerivedType(typeof(ApiRequest.LoadNoteXml), "loadNoteXml")]
[JsonDerivedType(typeof(ApiRequest.ImportFile), "importFile")]
[JsonDerivedType(typeof(ApiRequest.Search), "search")]
[JsonDerivedType(typeof(ApiRequest.ResolveObject), "resolveObject")]
[JsonDerivedType(typeof(ApiRequest.ListObjects), "listObjects")]
public abstract record ApiRequest
{
    public sealed record OpenVault(string Root) : ApiRequest;
    public sealed record Scan : ApiRequest;
    public sealed record CreateXmlNote(string Title, string? Folder) : ApiRequest;
    public sealed record SaveNoteXml(Guid ObjectId, string Xml) : ApiRequest;
    public sealed record LoadNoteXml(Guid ObjectId) : ApiRequest;
    public sealed record ImportFile(string SourcePath, string? TargetFolder) : ApiRequest;
    public sealed record Search(string Query, int Limit) : ApiRequest;
    public sealed record ResolveObject(Guid ObjectId) : ApiRequest;
    public sealed record ListObjects : ApiRequest;
}
