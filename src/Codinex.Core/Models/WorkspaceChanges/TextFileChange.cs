using System;

namespace Codinex.Core.Models.WorkspaceChanges;

/// <summary>
/// Represents a text modification inside a file.
/// </summary>
public sealed class TextFileChange
{
    public Guid Id { get; set; }

    public int Order { get; set; }

    public string FilePath { get; set; }

    public string Operation { get; set; }

    public string Target { get; set; }

    public string Search { get; set; }

    public string Content { get; set; }
}
