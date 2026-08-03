namespace Codinex.Infrastructure.WorkspaceChanges.Parsing.Dtos;

/// <summary>
/// Represents a move file change.
/// </summary>
public sealed class MoveFileChangeDto : WorkspaceChangeDto
{
    public string SourcePath { get; set; } = string.Empty;

    public string DestinationPath { get; set; } = string.Empty;
}