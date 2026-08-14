using System;

namespace Codinex.Infrastructure.WorkspaceChanges.Parsing.Dtos;

/// <summary>
/// Represents a text replacement inside a file.
/// </summary>
public sealed class TextFileChangeDto
{
    public Guid Id { get; set; }

    public int Order { get; set; }

    public string Before { get; set; } = string.Empty;

    public string Search { get; set; } = string.Empty;

    public string Replace { get; set; } = string.Empty;

    public string After { get; set; } = string.Empty;
}
