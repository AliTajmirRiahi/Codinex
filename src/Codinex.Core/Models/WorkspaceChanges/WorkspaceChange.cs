using System;
using Newtonsoft.Json;

namespace Codinex.Core.Models.WorkspaceChanges;

/// <summary>
/// Represents a workspace modification.
/// </summary>
[JsonConverter(typeof(WorkspaceChangeConverter))]
public abstract class WorkspaceChange
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public abstract WorkspaceChangeKind Kind { get; }
}
