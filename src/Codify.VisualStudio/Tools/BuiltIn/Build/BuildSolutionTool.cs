using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Conversation;
using Codify.Core.Models;
using Codify.Core.Tools;
using Codify.VisualStudio.Interfaces;

namespace Codify.VisualStudio.Tools.BuiltIn.Build;

/// <summary>
/// Builds the current Visual Studio solution.
/// </summary>
public sealed class BuildSolutionTool(
    IBuildService buildService)
    : IAiTool
{
    public string Name => "build_solution";

    public string Description =>
        "Builds the current Visual Studio solution and returns the build result.";

    public ToolVisibility Visibility { get; } = ToolVisibility.Model;

    public ToolDefinition Definition => new(
        new Dictionary<string, ToolProperty>(),
        []);

    public async Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken)
    {
        var result = await buildService.BuildSolutionAsync(cancellationToken);

        return ToolResult.Successful(
            request.Id,
            result);
    }
}