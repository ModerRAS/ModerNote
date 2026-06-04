using System;
using System.Linq;
using System.Threading.Tasks;
using Modernote.Client;
using Modernote.Core.Model;
using Modernote.Core.Xml;
using Modernote.Protocol;

namespace Modernote.Desktop;

public sealed class DesktopRuntime
{
    private readonly ModernoteClient _client;
    public DesktopState State { get; } = new();

    public DesktopRuntime(ModernoteClient client) => _client = client;

    public async Task OpenVaultAsync(string path)
    {
        var root = await _client.OpenVaultAsync(path);
        State.VaultRoot = root;
        State.StatusMessage = $"Vault: {root}";
        await ScanAsync();
    }

    public async Task ScanAsync()
    {
        var count = await _client.ScanAsync();
        var objects = await _client.ListObjectsAsync();
        State.Objects.Clear();
        foreach (var obj in objects) State.Objects.Add(obj);
        State.StatusMessage = $"Indexed {count} objects";
    }

    public async Task SelectAsync(ObjectDto obj)
    {
        State.SelectedObject = obj;
        if (obj.Kind == ObjectKind.XmlNote)
        {
            try
            {
                var xml = await _client.LoadNoteAsync(obj.Id);
                State.EditorXml = xml;
                if (State.Host != null)
                {
                    var doc = XmlNoteSerializer.Parse(xml);
                    State.Host.LoadDocument(doc);
                }
            }
            catch (Exception ex)
            {
                State.EditorXml = $"<error>{ex.Message}</error>";
            }
        }
        else
        {
            State.EditorXml = string.Empty;
            State.Host?.Blocks.Clear();
        }
    }

    public async Task SaveCurrentNoteAsync()
    {
        if (State.SelectedObject == null || State.Host == null) return;
        var doc = State.Host.SaveDocument();
        var xml = XmlNoteSerializer.Serialize(doc);
        await _client.SaveNoteAsync(State.SelectedObject.Id, xml);
        State.EditorXml = xml;
        State.IsDirty = false;
        State.StatusMessage = "Saved";
    }

    public async Task CreateNoteAsync(string title)
    {
        var note = await _client.CreateNoteAsync(title, null);
        await ScanAsync();
        await SelectAsync(note);
    }
}
