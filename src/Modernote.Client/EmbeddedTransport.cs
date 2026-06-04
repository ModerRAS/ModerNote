using System;
using System.Threading.Tasks;
using Modernote.Protocol;
using Modernote.Service;

namespace Modernote.Client;

public sealed class EmbeddedTransport : ITransport
{
    private readonly ModernoteService _service;
    public EmbeddedTransport(ModernoteService service) => _service = service;

    public Task<ApiResponse> SendAsync(ApiRequest request) =>
        Task.FromResult(Dispatch(request));

    private ApiResponse Dispatch(ApiRequest req) => req switch
    {
        ApiRequest.OpenVault r => _service.OpenVault(r.Root),
        ApiRequest.Scan => _service.Scan(),
        ApiRequest.CreateXmlNote r => _service.CreateXmlNote(r.Title, r.Folder),
        ApiRequest.SaveNoteXml r => _service.SaveNoteXml(r.ObjectId, r.Xml),
        ApiRequest.LoadNoteXml r => _service.LoadNoteXml(r.ObjectId),
        ApiRequest.ImportFile r => _service.ImportFile(r.SourcePath, r.TargetFolder),
        ApiRequest.Search r => _service.Search(r.Query, r.Limit),
        ApiRequest.ResolveObject r => _service.ResolveObject(r.ObjectId),
        ApiRequest.ListObjects => _service.ObjectsListed(),
        _ => throw new NotImplementedException($"Unknown request: {req.GetType().Name}")
    };
}
