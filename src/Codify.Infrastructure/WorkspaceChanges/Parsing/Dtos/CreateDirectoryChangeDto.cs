namespace Codify.Infrastructure.WorkspaceChanges.Parsing.Dtos;

/// <summary>
/// Represents a creation directory change.
/// </summary>
public sealed class CreateDirectoryChangeDto : WorkspaceChangeDto
{
    public string Path { get; set; } = string.Empty;
}