namespace Codify.Infrastructure.WorkspaceChanges.Parsing.Dtos;

/// <summary>
/// Represents a rename file change.
/// </summary>
public sealed class RenameFileChangeDto : WorkspaceChangeDto
{
    public string FilePath { get; set; } = string.Empty;

    public string NewFileName { get; set; } = string.Empty;
}