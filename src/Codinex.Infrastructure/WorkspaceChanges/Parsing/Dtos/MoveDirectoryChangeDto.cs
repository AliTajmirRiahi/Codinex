namespace Codify.Infrastructure.WorkspaceChanges.Parsing.Dtos;

/// <summary>
/// Represents a move directory change.
/// </summary>
public sealed class MoveDirectoryChangeDto : WorkspaceChangeDto
{
    public string SourcePath { get; set; } = string.Empty;

    public string DestinationPath { get; set; } = string.Empty;
}