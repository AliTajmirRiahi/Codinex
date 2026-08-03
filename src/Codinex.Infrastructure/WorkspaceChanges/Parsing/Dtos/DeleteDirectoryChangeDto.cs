namespace Codinex.Infrastructure.WorkspaceChanges.Parsing.Dtos;

/// <summary>
/// Represents a delete directory change.
/// </summary>
public sealed class DeleteDirectoryChangeDto : WorkspaceChangeDto
{
    public string DirectoryPath { get; set; } = string.Empty;
}