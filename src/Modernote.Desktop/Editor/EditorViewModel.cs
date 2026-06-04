using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Modernote.Core.Model;
using Modernote.Core.Xml;

namespace Modernote.Desktop.Editor;

public partial class EditorViewModel : ObservableObject
{
    [ObservableProperty] private string xml = string.Empty;
    [ObservableProperty] private DocumentRoot? document;
    [ObservableProperty] private Guid? currentNoteId;
    [ObservableProperty] private bool isDirty;

    public ObservableCollection<BlockViewModel> Blocks { get; } = new();

    public void LoadXml(string xmlContent)
    {
        Xml = xmlContent;
        Document = XmlNoteSerializer.Parse(xmlContent);
        Blocks.Clear();
        foreach (var block in Document.Children)
            Blocks.Add(BlockViewModelFactory.Create(block));
        IsDirty = false;
    }

    public string Serialize()
    {
        if (Document == null) return Xml;
        Document = Document with { Children = Blocks.Select(b => b.Block).ToList() };
        return XmlNoteSerializer.Serialize(Document);
    }
}
