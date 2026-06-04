using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Modernote.Protocol;

namespace Modernote.Client;

public sealed class ModernoteClient
{
    private readonly ITransport _transport;
    public ModernoteClient(ITransport transport) => _transport = transport;

    public async Task<string> OpenVaultAsync(string rootPath) =>
        Assert<ApiResponse.VaultOpened>(await _transport.SendAsync(new ApiRequest.OpenVault(rootPath))).Root;

    public async Task<int> ScanAsync() =>
        Assert<ApiResponse.ScanCompleted>(await _transport.SendAsync(new ApiRequest.Scan())).ObjectsIndexed;

    public async Task<ObjectDto> CreateNoteAsync(string title, string? folder) =>
        Assert<ApiResponse.XmlNoteCreated>(await _transport.SendAsync(new ApiRequest.CreateXmlNote(title, folder))).Object;

    public async Task<ObjectDto> SaveNoteAsync(Guid id, string xml) =>
        Assert<ApiResponse.NoteSaved>(await _transport.SendAsync(new ApiRequest.SaveNoteXml(id, xml))).Object;

    public async Task<string> LoadNoteAsync(Guid id) =>
        Assert<ApiResponse.NoteXmlLoaded>(await _transport.SendAsync(new ApiRequest.LoadNoteXml(id))).Xml;

    public async Task<ObjectDto> ImportFileAsync(string source, string? folder) =>
        Assert<ApiResponse.FileImported>(await _transport.SendAsync(new ApiRequest.ImportFile(source, folder))).Object;

    public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(string query, int limit = 50) =>
        Assert<ApiResponse.SearchResults>(await _transport.SendAsync(new ApiRequest.Search(query, limit))).Results;

    public async Task<ObjectDto?> ResolveObjectAsync(Guid id) =>
        Assert<ApiResponse.ObjectResolved>(await _transport.SendAsync(new ApiRequest.ResolveObject(id))).Object;

    public async Task<IReadOnlyList<ObjectDto>> ListObjectsAsync() =>
        Assert<ApiResponse.ObjectsListed>(await _transport.SendAsync(new ApiRequest.ListObjects())).Objects;

    private static T Assert<T>(ApiResponse r) where T : ApiResponse =>
        r is T t ? t : throw new InvalidOperationException($"Unexpected response: {r.GetType().Name}");
}
