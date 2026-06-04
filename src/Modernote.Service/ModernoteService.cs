using System;
using System.IO;
using Modernote.Core.Vault;
using Modernote.Protocol;

namespace Modernote.Service;

public sealed class ModernoteService
{
    private Vault? _vault;
    public string? RootPath { get; private set; }
    public bool IsOpen => _vault != null;

    public ApiResponse OpenVault(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            return new ApiResponse.VaultOpened("(invalid)");
        RootPath = Path.GetFullPath(rootPath);
        _vault = Vault.OpenOrCreate(RootPath);
        return new ApiResponse.VaultOpened(RootPath);
    }

    public ApiResponse Scan() => new ApiResponse.ScanCompleted(0);
    public ApiResponse CreateXmlNote(string title, string? folder) =>
        new ApiResponse.XmlNoteCreated(MakeDto(ObjectKind.XmlNote, title), "");
    public ApiResponse SaveNoteXml(Guid id, string xml) =>
        new ApiResponse.NoteSaved(MakeDto(ObjectKind.XmlNote, "", id));
    public ApiResponse LoadNoteXml(Guid id) =>
        new ApiResponse.NoteXmlLoaded(MakeDto(ObjectKind.XmlNote, "", id), "");
    public ApiResponse ImportFile(string source, string? folder) =>
        new ApiResponse.FileImported(MakeDto(ObjectKind.Other, Path.GetFileName(source)));
    public ApiResponse Search(string query, int limit) =>
        new ApiResponse.SearchResults(Array.Empty<SearchResultDto>());
    public ApiResponse ResolveObject(Guid id) => new ApiResponse.ObjectResolved(null);
    public ApiResponse ObjectsListed() =>
        new ApiResponse.ObjectsListed(Array.Empty<ObjectDto>());

    private static ObjectDto MakeDto(ObjectKind kind, string name, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), kind, name, name, "", "text/plain", 0,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    public void Close()
    {
        _vault?.Dispose();
        _vault = null;
        RootPath = null;
    }
}
