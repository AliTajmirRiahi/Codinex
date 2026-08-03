using System;

namespace Codinex.Core.Models.WorkspaceChanges;

public sealed class WorkspaceValidationError(
    Guid changeId,
    WorkspaceChangeErrorCode code,
    WorkspaceValidationCategory category,
    string message)
{
    public Guid ChangeId { get; set; } = changeId;

    public WorkspaceChangeErrorCode Code { get; set; } = code;

    public WorkspaceValidationCategory Category { get; set; } = category;

    public string Message { get; set; } = message;
}