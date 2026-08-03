using Codinex.Infrastructure.WorkspaceChanges.Parsing.Converters;
using Newtonsoft.Json;

namespace Codinex.Infrastructure.WorkspaceChanges.Parsing.Dtos;

/// <summary>
/// Represents the base type for all workspace change DTOs.
/// </summary>
[JsonConverter(typeof(WorkspaceChangeDtoConverter))]
public abstract class WorkspaceChangeDto
{
}