using System;
using System.Collections.Generic;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.VisualStudio.Tools.BuiltIn.Workspace;

/// <summary>
/// A changeset review persisted to disk (solution-scoped) so it survives
/// closing the Code Changes tool window or restarting Visual Studio. Holds
/// everything needed to re-show the diff and, later, apply the user's
/// decision without the original AI conversation still being alive.
/// </summary>
public sealed class PendingChangesetRecord
{
    public Guid Id { get; set; }

    public string Summary { get; set; }

    public WorkspaceChangeSet Changes { get; set; }

    public IReadOnlyList<ResolvedFileChange> ResolvedChanges { get; set; } = [];

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
