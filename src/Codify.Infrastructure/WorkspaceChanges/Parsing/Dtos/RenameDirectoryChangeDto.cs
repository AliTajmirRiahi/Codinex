namespace Codify.Infrastructure.WorkspaceChanges.Parsing.Dtos;

/// <summary>
/// Represents a rename directory change.
/// </summary>
public sealed class RenameDirectoryChangeDto : WorkspaceChangeDto
{
    public string Path { get; set; } = string.Empty;

    public string NewName { get; set; } = string.Empty;
}