using System.Text.Json.Serialization;

namespace Modernote.Protocol;

/// <summary>Asynchronous events from service to client.</summary>
[JsonDerivedType(typeof(ServiceEvent.ObjectChanged), "objectChanged")]
[JsonDerivedType(typeof(ServiceEvent.ScanCompleted), "scanCompleted")]
[JsonDerivedType(typeof(ServiceEvent.IndexingFailed), "indexingFailed")]
public abstract record ServiceEvent
{
    public sealed record ObjectChanged(ObjectDto Object) : ServiceEvent;
    public sealed record ScanCompleted(int ObjectsIndexed) : ServiceEvent;
    public sealed record IndexingFailed(string LogicalPath, string Message) : ServiceEvent;
}
