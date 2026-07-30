namespace Codify.Infrastructure.WorkspaceChanges.Parsing.Dtos;

/// <summary>
/// Represents a rename file change.
/// </summary>
public sealed class RenameFileChangeDto : WorkspaceChangeDto
{
    public string Path { get; set; } = string.Empty;

    public string NewName { get; set; } = string.Empty;
}