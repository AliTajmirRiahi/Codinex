using System.Collections.Generic;

namespace Codinex.Core.Models.WorkspaceChanges;

/// <summary>
/// Result of parsing an AI response into a <see cref="WorkspaceChangeSet"/>.
/// Parse failures are reported as <see cref="Errors"/> rather than thrown, so callers
/// can feed them back to the AI model as a tool failure instead of crashing the turn.
/// </summary>
public sealed class WorkspaceChangeParseResult
{
    public bool Success => Errors.Count == 0;

    public WorkspaceChangeSet ChangeSet { get; private set; }

    public List<WorkspaceValidationError> Errors { get; } = [];

    public static WorkspaceChangeParseResult Successful(WorkspaceChangeSet changeSet)
    {
        return new WorkspaceChangeParseResult { ChangeSet = changeSet };
    }

    public static WorkspaceChangeParseResult Failed(WorkspaceValidationError error)
    {
        var result = new WorkspaceChangeParseResult();

        result.Errors.Add(error);

        return result;
    }
}
