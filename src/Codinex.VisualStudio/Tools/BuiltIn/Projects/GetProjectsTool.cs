using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Context;
using Codinex.Core.Models;
using Codinex.Core.Models.Tools;
using Codinex.Core.Tools;

namespace Codinex.VisualStudio.Tools.BuiltIn.Projects;

/// <summary>
/// Gets project information for the current Visual Studio solution.
/// </summary>
[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class GetProjectsTool(
    IProjectContextProvider projectContextProvider)
    : IAiTool
{
    public string Name => "get_projects";

    public string Description =>
        "Gets project information for the current Visual Studio solution.";

    public IReadOnlyList<string> Capabilities =>
    [
        "get projects",
        "list projects",
        "show projects",
        "solution projects",
        "project information",
        "workspace projects"
    ];

    public string StatusMessage => "Getting projects...";

    public ToolVisibility Visibility { get; } = ToolVisibility.Model;

    public ToolDefinition Definition => new(
        new Dictionary<string, ToolProperty>(),
        []);

    public async Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var context =
                await projectContextProvider.GetContextAsync(cancellationToken);

            var projects = context?.Projects ?? [];

            return ToolResult.Successful(
                request.Id,
                new
                {
                    context?.SolutionName,
                    context?.SolutionPath,
                    context?.SolutionDirectory,
                    context?.StartupProjects,
                    context?.Configuration,
                    context?.Platform,
                    Count = projects.Count,
                    Projects = projects
                        .OrderBy(project => project.Name)
                        .Select(project => new
                        {
                            project.Name,
                            project.FullPath,
                            project.RelativePath,
                            project.TargetFramework,
                            project.OutputType,
                            project.AssemblyName,
                            project.RootNamespace
                        })
                });
        }
        catch (Exception ex)
        {
            return ToolResult.Failed(
                request.Id,
                ex.Message);
        }
    }
}
