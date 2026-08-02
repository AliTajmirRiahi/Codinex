using Codify.Core.Conversation;
using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Models;
using Codify.Core.Tools;
using Codify.VisualStudio.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models.Tools;

namespace Codify.VisualStudio.Tools.BuiltIn.Build;

/// <summary>
/// Builds the current Visual Studio solution.
/// </summary>
[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class BuildSolutionTool(
    IBuildService buildService)
    : IAiTool
{
    public string Name => "build_solution";

    public string Description =>
        "Builds the current Visual Studio solution and returns the build result.";

    public string StatusMessage => "Building solution...";

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