using System.Collections.Generic;

namespace Codinex.VisualStudio.Tools.BuiltIn.Workspace;

/// <summary>
/// The user's per-file accept/reject decision for a reviewed change set, plus
/// an optional reason (only meaningful when at least one file was rejected)
/// that gets relayed back to the model.
/// </summary>
public sealed class ChangesetDecision
{
    /// <summary>Change path (see <see cref="WorkspaceChangePathResolver"/>) -> approved.</summary>
    public Dictionary<string, bool> FileDecisions { get; set; } = new();

    public string Reason { get; set; }
}
