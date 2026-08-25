using System;
using System.Linq;
using System.Text;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Context;
using Codinex.Core.Models;

namespace Codinex.VisualStudio.Workspace.Formatters;

/// <summary>
/// Formats project context into a prompt-friendly markdown document.
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Features)]
public sealed class ProjectContextFormatter : IProjectContextFormatter
{
    public string Format(ProjectContext context)
    {
        _ = context ?? throw new ArgumentNullException(nameof(context));

        var builder = new StringBuilder();

        builder.AppendLine("# Solution");
        builder.AppendLine();

        builder.AppendLine($"Name: {context.SolutionName ?? "(Unknown)"}");

        if (!string.IsNullOrWhiteSpace(context.Configuration))
        {
            builder.AppendLine($"Configuration: {context.Configuration}");
        }

        if (!string.IsNullOrWhiteSpace(context.Platform))
        {
            builder.AppendLine($"Platform: {context.Platform}");
        }

        if (context.StartupProjects?.Count > 0)
        {
            builder.AppendLine($"Startup Projects: {string.Join(", ", context.StartupProjects)}");
        }

        builder.AppendLine();

        builder.AppendLine("# Projects");
        builder.AppendLine();

        if (context.Projects == null || context.Projects.Count == 0)
        {
            builder.AppendLine("(No projects found.)");
            return builder.ToString();
        }

        foreach (var project in context.Projects.OrderBy(x => x.Name))
        {
            builder.AppendLine($"## {project.Name}");

            if (!string.IsNullOrWhiteSpace(project.RelativePath))
            {
                builder.AppendLine($"Path: {project.RelativePath}");
            }

            if (!string.IsNullOrWhiteSpace(project.TargetFramework))
            {
                builder.AppendLine($"Framework: {project.TargetFramework}");
            }

            if (!string.IsNullOrWhiteSpace(project.OutputType))
            {
                builder.AppendLine($"Output: {project.OutputType}");
            }

            if (!string.IsNullOrWhiteSpace(project.AssemblyName))
            {
                builder.AppendLine($"Assembly: {project.AssemblyName}");
            }

            if (!string.IsNullOrWhiteSpace(project.RootNamespace))
            {
                builder.AppendLine($"Namespace: {project.RootNamespace}");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }
}