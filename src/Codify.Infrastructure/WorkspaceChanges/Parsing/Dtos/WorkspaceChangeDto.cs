using Codify.Infrastructure.WorkspaceChanges.Parsing.Converters;
using Newtonsoft.Json;

namespace Codify.Infrastructure.WorkspaceChanges.Parsing.Dtos;

/// <summary>
/// Represents the base type for all workspace change DTOs.
/// </summary>
[JsonConverter(typeof(WorkspaceChangeDtoConverter))]
public abstract class WorkspaceChangeDto
{
}