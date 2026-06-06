namespace Codify.Core.Models;

public enum AttachmentType
{
    CodeSnippet,
    File,
    Image,
    Selection 
}

public sealed class Attachment
{
    public AttachmentType Type { get; set; }
    public string Content { get; set; } 
    public string? FileName { get; set; }
    public byte[]? RawData { get; set; } 
}