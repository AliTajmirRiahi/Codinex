using Codinex.Core.Models;
using System;
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
/// Builds a project in the current Visual Studio solution by name.
/// </summary>
[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class BuildProjectTool(
    IBuildService buildService)
    : IAiTool
{
    public string Name => "build_project";

    public string Description =>
        "Builds one project in the current Visual Studio solution by project name.";

    public string StatusMessage => "Building project...";

    public ToolVisibility Visibility { get; } = ToolVisibility.Model;

    public ToolDefinition Definition => new(
        new Dictionary<string, ToolProperty>
        {
            ["projectName"] = new(
                ToolPropertyType.String,
                "The name of the project to build.")
        },
        ["projectName"]);

    public async Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var projectName = request.GetRequiredString("projectName");

            var result = await buildService.BuildProjectAsync(
                projectName,
                cancellationToken);

            return ToolResult.Successful(
                request.Id,
                result);
        }
        catch (Exception ex)
        {
            return ToolResult.Failed(
                request.Id,
                ex.Message);
        }
    }
}
