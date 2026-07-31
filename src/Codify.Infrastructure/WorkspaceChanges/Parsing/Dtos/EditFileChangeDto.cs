using System.Collections.Generic;

namespace Codify.Infrastructure.WorkspaceChanges.Parsing.Dtos;

/// <summary>
/// Represents an edit file change.
/// </summary>
public sealed class EditFileChangeDto : WorkspaceChangeDto
{
    public string FilePath { get; set; } = string.Empty;

    public List<TextFileChangeDto> TextChanges { get; set; } = [];
}