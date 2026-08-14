using System;

namespace Codinex.Core.Models.WorkspaceChanges;

/// <summary>
/// Represents a text modification inside a file.
/// </summary>
public sealed class TextFileChange
{
    public Guid Id { get; set; }

    public int Order { get; set; }

    public string Before { get; set; }

    public string Search { get; set; }

    public string Replace { get; set; }

    public string After { get; set; }
}
