using System.Collections.Generic;

namespace Codinex.Infrastructure.WorkspaceChanges.Parsing.Dtos;

/// <summary>
/// Represents a collection of workspace changes returned by the AI tool.
/// </summary>
public sealed class WorkspaceChangeSetDto
{
    public List<WorkspaceChangeDto> Changes { get; set; } = [];
}