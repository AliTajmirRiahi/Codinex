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

    /// <summary>
    /// True when this "decision" is not actually a decision — it was produced
    /// because the review's tool window or Visual Studio itself closed before
    /// the user chose. The review stays pending (nothing is applied or
    /// discarded, persistence is untouched) so it can be picked up again later.
    /// </summary>
    public bool IsUndecided { get; set; }

    public static ChangesetDecision Undecided() => new() { IsUndecided = true };
}
