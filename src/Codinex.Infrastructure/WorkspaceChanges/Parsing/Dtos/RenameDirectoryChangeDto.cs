namespace Codinex.Infrastructure.WorkspaceChanges.Parsing.Dtos;

/// <summary>
/// Represents a rename directory change.
/// </summary>
public sealed class RenameDirectoryChangeDto : WorkspaceChangeDto
{
    public string OldPath { get; set; } = string.Empty;

    public string NewPath { get; set; } = string.Empty;
}