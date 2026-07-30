namespace Codify.Infrastructure.WorkspaceChanges.Parsing.Dtos;

/// <summary>
/// Represents a creation file change.
/// </summary>
public sealed class CreateFileChangeDto : WorkspaceChangeDto
{
    public string Path { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}