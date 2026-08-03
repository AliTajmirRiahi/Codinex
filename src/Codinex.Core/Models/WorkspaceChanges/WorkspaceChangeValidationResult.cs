using System.Collections.Generic;

namespace Codinex.Core.Models.WorkspaceChanges;

/// <summary>
/// Represents the result of validating a workspace change set.
/// </summary>
public sealed class WorkspaceChangeValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public List<WorkspaceChangeValidationError> Errors { get; } = new();
}