using System;

namespace Codify.Core.Models.WorkspaceChanges;

/// <summary>
/// Represents a workspace modification.
/// </summary>
public abstract class WorkspaceChange
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public abstract WorkspaceChangeKind Kind { get; }
}
