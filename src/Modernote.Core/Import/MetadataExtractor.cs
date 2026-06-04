using System;
using System.IO;
using Modernote.Protocol;

namespace Modernote.Core.Import;

public static class MetadataExtractor
{
    /// <summary>Detect the ObjectKind from a file extension and logical path.</summary>
    public static ObjectKind DetectKind(string logicalPath, string actualPath)
    {
        if (logicalPath.StartsWith("notes/", StringComparison.OrdinalIgnoreCase))
        {
            var ext = Path.GetExtension(actualPath).ToLowerInvariant();
            if (ext == ".xml" || ext == ".html" || ext == ".htm")
                return ObjectKind.XmlNote;
        }

        var ext2 = Path.GetExtension(actualPath).ToLowerInvariant().TrimStart('.');
        return ext2 switch
        {
            "pdf" => ObjectKind.Pdf,
            "docx" => ObjectKind.Docx,
            "png" or "jpg" or "jpeg" or "gif" or "webp" or "bmp" or "svg" => ObjectKind.Image,
            "mp3" or "wav" or "flac" or "ogg" or "m4a" => ObjectKind.Audio,
            "mp4" or "mov" or "mkv" or "webm" or "avi" => ObjectKind.Video,
            "txt" or "log" or "csv" or "tsv" or "md" => ObjectKind.Text,
            "cs" or "js" or "ts" or "tsx" or "jsx" or "json" or "rs" => ObjectKind.Code,
            _ => ObjectKind.Other
        };
    }

    /// <summary>Detect MIME type for a file.</summary>
    public static string DetectMime(ObjectKind kind, string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return (kind, ext) switch
        {
            (ObjectKind.XmlNote, _) => "text/xml",
            (ObjectKind.Image, ".png") => "image/png",
            (ObjectKind.Image, ".jpg" or ".jpeg") => "image/jpeg",
            (ObjectKind.Image, ".gif") => "image/gif",
            (ObjectKind.Image, ".webp") => "image/webp",
            (ObjectKind.Image, ".bmp") => "image/bmp",
            (ObjectKind.Image, ".svg") => "image/svg+xml",
            (ObjectKind.Pdf, _) => "application/pdf",
            (ObjectKind.Docx, _) => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            (ObjectKind.Audio, ".mp3") => "audio/mpeg",
            (ObjectKind.Audio, ".wav") => "audio/wav",
            (ObjectKind.Audio, ".ogg") => "audio/ogg",
            (ObjectKind.Audio, ".flac") => "audio/flac",
            (ObjectKind.Audio, ".m4a") => "audio/mp4",
            (ObjectKind.Video, ".mp4") => "video/mp4",
            (ObjectKind.Video, ".webm") => "video/webm",
            (ObjectKind.Video, ".mov") => "video/quicktime",
            (ObjectKind.Text, _) => "text/plain",
            (ObjectKind.Code, ".json") => "application/json",
            (ObjectKind.Code, _) => "text/plain",
            _ => "application/octet-stream"
        };
    }
}
