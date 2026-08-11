using System;

namespace Codinex.Infrastructure.WorkspaceChanges.Parsing.Dtos;

/// <summary>
/// Represents a text modification inside a file.
/// </summary>
public sealed class TextFileChangeDto
{
    public Guid Id { get; set; }

    public int Order { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public string Operation { get; set; } = string.Empty;

    public string Target { get; set; } = string.Empty;

    public string Search { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}
