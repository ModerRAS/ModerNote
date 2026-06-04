using System;
using System.IO;
using System.Linq;
using Modernote.Core.Search;
using Modernote.Core.Vault;
using Modernote.Protocol;

namespace Modernote.Service;

/// <summary>Service boundary that wraps the Vault. All methods return ApiResponse.</summary>
public sealed class ModernoteService
{
    private Vault? _vault;
    public string? RootPath { get; private set; }
    public bool IsOpen => _vault != null;

    public ApiResponse OpenVault(string rootPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                return new ApiResponse.VaultOpened("(invalid)");
            RootPath = Path.GetFullPath(rootPath);
            _vault?.Dispose();
            _vault = Vault.OpenOrCreate(RootPath);
            return new ApiResponse.VaultOpened(RootPath);
        }
        catch (Exception ex)
        {
            return new ApiResponse.VaultOpened($"error: {ex.Message}");
        }
    }

    public ApiResponse Scan()
    {
        try
        {
            if (_vault == null) return new ApiResponse.ScanCompleted(0);
            var summary = _vault.Scan();
            return new ApiResponse.ScanCompleted(summary.ObjectsIndexed);
        }
        catch (Exception)
        {
            return new ApiResponse.ScanCompleted(-1);
        }
    }

    public ApiResponse CreateXmlNote(string title, string? folder)
    {
        try
        {
            if (_vault == null)
                return new ApiResponse.XmlNoteCreated(EmptyDto(ObjectKind.XmlNote), "");
            var dto = _vault.CreateXmlNote(title, folder);
            var (_, xml) = _vault.LoadNoteXml(dto.Id);
            return new ApiResponse.XmlNoteCreated(dto, xml);
        }
        catch (Exception)
        {
            return new ApiResponse.XmlNoteCreated(EmptyDto(ObjectKind.XmlNote), "");
        }
    }

    public ApiResponse SaveNoteXml(Guid objectId, string xml)
    {
        try
        {
            if (_vault == null) return new ApiResponse.NoteSaved(EmptyDto(ObjectKind.XmlNote));
            var dto = _vault.SaveNoteXml(objectId, xml);
            return new ApiResponse.NoteSaved(dto);
        }
        catch (Exception)
        {
            return new ApiResponse.NoteSaved(EmptyDto(ObjectKind.XmlNote));
        }
    }

    public ApiResponse LoadNoteXml(Guid objectId)
    {
        try
        {
            if (_vault == null) return new ApiResponse.NoteXmlLoaded(EmptyDto(ObjectKind.XmlNote), "");
            var (dto, xml) = _vault.LoadNoteXml(objectId);
            return new ApiResponse.NoteXmlLoaded(dto, xml);
        }
        catch (Exception)
        {
            return new ApiResponse.NoteXmlLoaded(EmptyDto(ObjectKind.XmlNote), "");
        }
    }

    public ApiResponse ImportFile(string sourcePath, string? targetFolder)
    {
        try
        {
            if (_vault == null) return new ApiResponse.FileImported(EmptyDto(ObjectKind.Other));
            var dto = _vault.ImportFile(sourcePath, targetFolder);
            return new ApiResponse.FileImported(dto);
        }
        catch (Exception)
        {
            return new ApiResponse.FileImported(EmptyDto(ObjectKind.Other));
        }
    }

    public ApiResponse Search(string query, int limit)
    {
        try
        {
            if (_vault == null) return new ApiResponse.SearchResults(Array.Empty<SearchResultDto>());
            var service = new Core.Search.SearchService(_vault.Connection);
            var results = service.Search(query, limit);
            return new ApiResponse.SearchResults(results);
        }
        catch (Exception)
        {
            return new ApiResponse.SearchResults(Array.Empty<SearchResultDto>());
        }
    }

    public ApiResponse ResolveObject(Guid objectId)
    {
        try
        {
            if (_vault == null) return new ApiResponse.ObjectResolved(null);
            var dto = _vault.ResolveObject(objectId);
            return new ApiResponse.ObjectResolved(dto);
        }
        catch (Exception)
        {
            return new ApiResponse.ObjectResolved(null);
        }
    }

    public ApiResponse ObjectsListed()
    {
        try
        {
            if (_vault == null) return new ApiResponse.ObjectsListed(Array.Empty<ObjectDto>());
            return new ApiResponse.ObjectsListed(_vault.ListObjects());
        }
        catch (Exception)
        {
            return new ApiResponse.ObjectsListed(Array.Empty<ObjectDto>());
        }
    }

    public void Close()
    {
        _vault?.Dispose();
        _vault = null;
        RootPath = null;
    }

    private static ObjectDto EmptyDto(ObjectKind kind) => new(
        Guid.Empty, kind, "", "", "", "application/octet-stream", 0,
        DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
}
