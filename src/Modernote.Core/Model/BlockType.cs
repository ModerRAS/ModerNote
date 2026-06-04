namespace Modernote.Core.Model;

/// <summary>All 15 supported block types. Order matters for serialization determinism.</summary>
public enum BlockType
{
    H1, H2, H3, H4, H5, H6,           // 0-5: Headings
    Paragraph,                       // 6
    Image, Video, Audio, Pdf, File,  // 7-11: Media
    Quote,                           // 12
    Code,                            // 13
    Table,                           // 14
    Todo,                            // 15
    Details,                         // 16
    HorizontalRule,                  // 17
    Custom                           // 18
}
