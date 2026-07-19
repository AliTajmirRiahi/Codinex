using System;
using System.Collections.Generic;
using System.Linq;
using Codify.Core.Tools;

namespace Codify.VisualStudio.Tools;

/// <summary>
/// Default AI tool registry.
/// </summary>
public sealed class AiToolRegistry : IAiToolRegistry
{
    private readonly IReadOnlyDictionary<string, IAiTool> _tools;

    private readonly IReadOnlyList<IAiTool> _toolList;

    public AiToolRegistry(IEnumerable<IAiTool> tools)
    {
        var list = tools.ToList();

        _toolList = list.AsReadOnly();

        _tools = list.ToDictionary(
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

    public IReadOnlyList<IAiTool> GetAll()
    {
        return _toolList;
    }
}