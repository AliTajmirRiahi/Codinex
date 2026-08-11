using System;

namespace Codinex.Core.Models.WorkspaceChanges;

public sealed class ChangeValidationError(
    Guid changeId,
    WorkspaceChangeErrorCode code,
    WorkspaceValidationCategory category,
    string message)
{
    public Guid ChangeId { get; } = changeId;

    public WorkspaceChangeErrorCode Code { get; } = code;

    public WorkspaceValidationCategory Category { get; } = category;

    public string Message { get; } = message;
}
