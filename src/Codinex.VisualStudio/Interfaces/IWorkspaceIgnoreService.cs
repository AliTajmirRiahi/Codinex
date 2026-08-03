namespace Codinex.VisualStudio.Interfaces;

/// <summary>
/// Determines whether workspace files or directories should be ignored.
/// </summary>
public interface IWorkspaceIgnoreService
{
    /// <summary>
    /// Determines whether the specified file should be ignored.
    /// </summary>
    bool ShouldIgnore(string filePath);
}