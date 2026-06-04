using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Modernote.Core.Exceptions;
using Modernote.Protocol;

namespace Modernote.Core.Extraction;

public sealed record TextExtractionResult(string Body, string Extractor, string? Error = null);

public static class TextExtractor
{
    /// <summary>Extract text from a file based on its kind.</summary>
    public static TextExtractionResult Extract(ObjectKind kind, string filePath)
    {
        try
        {
            return kind switch
            {
                ObjectKind.XmlNote => new TextExtractionResult(ExtractXml(filePath), "xml"),
                ObjectKind.Docx => new TextExtractionResult(ExtractDocx(filePath), "docx"),
                ObjectKind.Pdf => new TextExtractionResult(ExtractPdf(filePath), "pdf"),
                ObjectKind.Text => new TextExtractionResult(File.ReadAllText(filePath), "plain_text"),
                ObjectKind.Code => new TextExtractionResult(File.ReadAllText(filePath), "plain_text"),
                _ => new TextExtractionResult(string.Empty, "metadata_only")
            };
        }
        catch (Exception ex)
        {
            return new TextExtractionResult(string.Empty, "failed", ex.Message);
        }
    }

    private static string ExtractXml(string path)
    {
        var doc = XDocument.Parse(File.ReadAllText(path));
        var sb = new StringBuilder();
        foreach (var node in doc.DescendantNodes().OfType<XText>())
        {
            sb.Append(node.Value);
            sb.Append(' ');
        }
        return sb.ToString().Trim();
    }

    private static string ExtractDocx(string path)
    {
        using var zip = ZipFile.OpenRead(path);
        var entry = zip.GetEntry("word/document.xml");
        if (entry == null) return string.Empty;
        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream);
        var doc = XDocument.Load(reader);
        var sb = new StringBuilder();
        foreach (var node in doc.DescendantNodes().OfType<XText>())
        {
            sb.Append(node.Value);
            sb.Append(' ');
        }
        return sb.ToString().Trim();
    }

    private static string ExtractPdf(string path)
    {
        // Minimal PDF text extraction: look for `BT...ET` text blocks and extract parens-quoted strings
        var bytes = File.ReadAllBytes(path);
        var text = Encoding.UTF8.GetString(bytes);
        var sb = new StringBuilder();
        var i = 0;
        while ((i = text.IndexOf("BT", i, StringComparison.Ordinal)) >= 0)
        {
            var end = text.IndexOf("ET", i, StringComparison.Ordinal);
            if (end < 0) break;
            var block = text.Substring(i, end - i);
            // Extract text in (parentheses) joined with Tj/TJ
            var inText = false;
            for (int k = 0; k < block.Length; k++)
            {
                var c = block[k];
                if (c == '(' && !inText) { inText = true; continue; }
                if (c == ')' && inText)
                {
                    inText = false;
                    sb.Append(' ');
                    continue;
                }
                if (inText && c != '\\' && c != '\n' && c != '\r') sb.Append(c);
            }
            i = end + 2;
        }
        return sb.ToString().Trim();
    }
}
