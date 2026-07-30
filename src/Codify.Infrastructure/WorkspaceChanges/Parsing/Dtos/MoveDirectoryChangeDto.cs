namespace Codify.Infrastructure.WorkspaceChanges.Parsing.Dtos;

/// <summary>
/// Represents a move directory change.
/// </summary>
public sealed class MoveDirectoryChangeDto : WorkspaceChangeDto
{
    public string Source { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;
}