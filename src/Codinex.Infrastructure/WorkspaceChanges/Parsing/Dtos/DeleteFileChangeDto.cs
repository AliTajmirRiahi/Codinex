namespace Codify.Infrastructure.WorkspaceChanges.Parsing.Dtos;

/// <summary>
/// Represents a delete file change.
/// </summary>
public sealed class DeleteFileChangeDto : WorkspaceChangeDto
{
    public string FilePath { get; set; } = string.Empty;
}