namespace Codify.Infrastructure.WorkspaceChanges.Parsing.Dtos;

/// <summary>
/// Represents a move file change.
/// </summary>
public sealed class MoveFileChangeDto : WorkspaceChangeDto
{
    public string Source { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;
}