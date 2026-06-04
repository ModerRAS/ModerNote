using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Modernote.Core.Exceptions;
using Modernote.Core.Model;

namespace Modernote.Core.Xml;

/// <summary>Parses and serializes XML note documents in the Modernote format.</summary>
public static class XmlNoteSerializer
{
    /// <summary>Parse XML string into a DocumentRoot.</summary>
    public static DocumentRoot Parse(string xml)
    {
        XDocument doc;
        try
        {
            doc = XDocument.Parse(xml);
        }
        catch (XmlException ex)
        {
            throw new DocumentFormatException($"Malformed XML: {ex.Message}", ex);
        }

        var root = doc.Root ?? throw new DocumentFormatException("XML has no root element");
        if (root.Name.LocalName != "document")
            throw new DocumentFormatException($"Expected <document> root, got <{root.Name.LocalName}>");
        int version = (int?)root.Attribute("version") ?? 1;
        var children = root.Elements().Select(ParseElement).ToList();
        return new DocumentRoot { Version = version, Children = children };
    }

    private static Block ParseElement(XElement e)
    {
        return e.Name.LocalName switch
        {
            "h1" => new HeadingBlock(1, e.Value.Trim()),
            "h2" => new HeadingBlock(2, e.Value.Trim()),
            "h3" => new HeadingBlock(3, e.Value.Trim()),
            "h4" => new HeadingBlock(4, e.Value.Trim()),
            "h5" => new HeadingBlock(5, e.Value.Trim()),
            "h6" => new HeadingBlock(6, e.Value.Trim()),
            "p" => new ParagraphBlock(e.Value),
            "quote" => new QuoteBlock(e.Value),
            "code" => new CodeBlock(e.Value, e.Attribute("language")?.Value),
            "todo" => new TodoBlock(e.Value.Trim(), e.Attribute("checked")?.Value == "true"),
            "image" => new ImageBlock(e.Attribute("src")?.Value ?? "", e.Attribute("caption")?.Value, ParseInt(e, "width")),
            "video" => new VideoBlock(e.Attribute("src")?.Value ?? ""),
            "audio" => new AudioBlock(e.Attribute("src")?.Value ?? ""),
            "pdf" => new PdfBlock(e.Attribute("src")?.Value ?? ""),
            "file" => new FileBlock(e.Attribute("src")?.Value ?? ""),
            "hr" => new HorizontalRuleBlock(),
            "table" => ParseTable(e),
            "details" => ParseDetails(e),
            "custom" => new CustomBlock(
                e.Attribute("type")?.Value ?? "",
                e.Attribute("src")?.Value,
                e.Attributes()
                    .Where(a => a.Name.LocalName != "type" && a.Name.LocalName != "src")
                    .ToDictionary(a => a.Name.LocalName, a => a.Value)),
            _ => throw new DocumentFormatException($"Unknown block type: <{e.Name.LocalName}>")
        };
    }

    private static TableBlock ParseTable(XElement e) => new(
        e.Elements("row").Select(row =>
            (IReadOnlyList<string>)row.Elements("cell").Select(c => c.Value).ToList()
        ).ToList()
    );

    private static DetailsBlock ParseDetails(XElement e) => new(
        e.Attribute("title")?.Value ?? "",
        e.Elements().Select(ParseElement).ToList()
    );

    private static int? ParseInt(XElement e, string name) =>
        int.TryParse(e.Attribute(name)?.Value, out var v) ? v : null;

    public static string Serialize(DocumentRoot doc, bool indented = true)
    {
        var root = new XElement("document", new XAttribute("version", doc.Version.ToString()));
        foreach (var block in doc.Children)
            root.Add(SerializeElement(block));
        var xdoc = new XDocument(root);
        return indented ? xdoc.ToString() : xdoc.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);
    }

    private static XElement SerializeElement(Block b) => b switch
    {
        HeadingBlock h => new XElement($"h{h.Level}", h.Text),
        ParagraphBlock p => new XElement("p", p.Text),
        QuoteBlock q => new XElement("quote", q.Text),
        CodeBlock c => new XElement("code", c.Code, AttrIf("language", c.Language)),
        TodoBlock t => new XElement("todo", new XAttribute("checked", t.Checked ? "true" : "false"), t.Text),
        ImageBlock i => new XElement("image", AttrIf("src", i.Source), AttrIf("caption", i.Caption), AttrIfInt("width", i.Width)),
        VideoBlock v => new XElement("video", new XAttribute("src", v.Source)),
        AudioBlock a => new XElement("audio", new XAttribute("src", a.Source)),
        PdfBlock p => new XElement("pdf", new XAttribute("src", p.Source)),
        FileBlock f => new XElement("file", new XAttribute("src", f.Source)),
        HorizontalRuleBlock => new XElement("hr"),
        TableBlock t => SerializeTable(t),
        DetailsBlock d => SerializeDetails(d),
        CustomBlock c => new XElement("custom", AttrIf("type", c.CustomType), AttrIf("src", c.Source)),
        _ => throw new DocumentFormatException($"Cannot serialize unknown block type: {b.GetType().Name}")
    };

    private static XElement SerializeTable(TableBlock t) => new("table",
        t.Rows.Select(row => new XElement("row",
            row.Select(cell => new XElement("cell", cell))
        ))
    );

    private static XElement SerializeDetails(DetailsBlock d)
    {
        var elem = new XElement("details", AttrIf("title", d.Title));
        foreach (var child in d.Children) elem.Add(SerializeElement(child));
        return elem;
    }

    private static XAttribute? AttrIf(string name, string? value) =>
        string.IsNullOrEmpty(value) ? null : new XAttribute(name, value);

    private static XAttribute? AttrIfInt(string name, int? value) =>
        value.HasValue ? new XAttribute(name, value.Value) : null;
}
