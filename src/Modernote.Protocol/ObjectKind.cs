namespace Modernote.Protocol;

/// <summary>Type discriminator for vault objects.</summary>
public enum ObjectKind
{
    XmlNote,
    Pdf,
    Docx,
    Image,
    Audio,
    Video,
    Text,
    Code,
    Other
}
