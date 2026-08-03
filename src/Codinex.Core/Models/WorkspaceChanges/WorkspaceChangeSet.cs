using System.Collections.Generic;

namespace Codinex.Core.Models.WorkspaceChanges;

/// <summary>
/// Represents a collection of file changes generated during a workspace modification.
/// </summary>
public sealed class WorkspaceChangeSet
{
    public List<WorkspaceChange> Changes { get; set; } = [];
}
