using System;
using System.Collections.Generic;
using System.Linq;
using Codify.Core.Tools;

namespace Codify.Infrastructure.Tools;

/// <summary>
/// Default AI tool registry.
/// </summary>
public sealed class AiToolRegistry : IAiToolRegistry
{
    private readonly IReadOnlyDictionary<string, IAiTool> _tools;

    public AiToolRegistry(IEnumerable<IAiTool> tools)
    {
        _tools = tools.ToDictionary(
            x => x.Name,
            StringComparer.OrdinalIgnoreCase);
    }

    public IAiTool Get(string toolName)
    {
        if (_tools.TryGetValue(toolName, out var tool))
            return tool;

        throw new InvalidOperationException(
            $"AI tool '{toolName}' is not registered.");
    }
}