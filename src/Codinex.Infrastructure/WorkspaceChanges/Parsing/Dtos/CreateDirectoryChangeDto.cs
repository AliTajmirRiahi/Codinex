namespace Codinex.Infrastructure.WorkspaceChanges.Parsing.Dtos;

/// <summary>
/// Represents a creation directory change.
/// </summary>
public sealed class CreateDirectoryChangeDto : WorkspaceChangeDto
{
    public string DirectoryPath { get; set; } = string.Empty;
}