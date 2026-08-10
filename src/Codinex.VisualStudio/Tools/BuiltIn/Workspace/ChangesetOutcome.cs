using Codinex.Core.Models.Tools;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.VisualStudio.Tools.BuiltIn.Workspace;

public enum ChangesetOutcomeKind
{
    Applied,
    Rejected,
    Undecided,
    Error
}

/// <summary>
/// The end result of a changeset review — what a caller (the live tool call,
/// or the resume-after-restart path) needs to report back.
/// </summary>
public sealed class ChangesetOutcome
{
    public ChangesetOutcomeKind Kind { get; private set; }

    public string Message { get; private set; }

    public WorkspaceChangeSuccess ChangeSuccess { get; private set; }

    public WorkspaceChangeError Error { get; private set; }

    public static ChangesetOutcome Applied(WorkspaceChangeSuccess success) =>
        new() { Kind = ChangesetOutcomeKind.Applied, ChangeSuccess = success, Message = success.Message };

    public static ChangesetOutcome Rejected(string message) =>
        new() { Kind = ChangesetOutcomeKind.Rejected, Message = message };

    public static ChangesetOutcome Undecided() =>
        new() { Kind = ChangesetOutcomeKind.Undecided };

    public static ChangesetOutcome Failed(string message, WorkspaceChangeError error) =>
        new() { Kind = ChangesetOutcomeKind.Error, Message = message, Error = error };
}
