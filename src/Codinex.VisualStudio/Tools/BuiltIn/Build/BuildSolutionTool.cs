using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Models.Tools;
using Codinex.Core.Tools;
using Codinex.VisualStudio.Interfaces;

namespace Codinex.VisualStudio.Tools.BuiltIn.Build;

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

    public IReadOnlyList<string> Capabilities =>
    [
        "build solution",
        "compile solution",
        "rebuild solution",
        "build workspace",
        "compile workspace",
        "solution build",
        "solution compile"
    ];

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