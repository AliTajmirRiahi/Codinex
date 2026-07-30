namespace Codify.Infrastructure.WorkspaceChanges.Parsing.Dtos;

/// <summary>
/// Represents a delete directory change.
/// </summary>
public sealed class DeleteDirectoryChangeDto : WorkspaceChangeDto
{
    public string Path { get; set; } = string.Empty;
}