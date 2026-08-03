namespace Codify.Core.Models.WorkspaceChanges;

/// <summary>
/// Represents a validation error for a workspace change.
/// </summary>
public sealed class WorkspaceChangeValidationError
{
    public WorkspaceChange Change { get; set; }

    public string Message { get; set; }
}