using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Context;
using Codinex.Core.Interfaces.Services;
using Codinex.Core.Interfaces.Workspace;
using Codinex.Core.Models.Context;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.Internal;

namespace Codinex.VisualStudio.Workspace.Providers;
#pragma warning disable VSTHRD010

/// <summary>
/// Provides information about the current solution and its projects.
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
public sealed class ProjectContextProvider(
    IVisualStudioServices visualStudio,
    IWorkspaceContext workspaceContext,
    IUiThreadDispatcher uiThreadDispatcher)
    : VsServiceBase(visualStudio), IProjectContextProvider
{
    private const string SolutionFolderKind = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";

    public async Task<ProjectContext> GetContextAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await uiThreadDispatcher.SwitchToMainThreadAsync();

        var dte = await GetDteAsync();

        if (dte?.Solution == null || !workspaceContext.IsSolutionOpen)
        {
            return new ProjectContext
            {
                Projects = []
            };
        }

        var projects = new List<ProjectContextItem>();

        foreach (var project in EnumerateProjects(dte.Solution.Projects))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var item = CreateProjectContextItem(project);

                if (item != null)
                {
                    projects.Add(item);
                }
            }
            catch (COMException)
            {
                // Ignore invalid COM projects.
            }
            catch
            {
                // Ignore unsupported project types.
            }
        }

        return new ProjectContext
        {
            SolutionName = workspaceContext.SolutionName,
            SolutionPath = workspaceContext.SolutionPath,
            SolutionDirectory = workspaceContext.SolutionDirectory,
            StartupProjects = workspaceContext.StartupProjects,
            Configuration = workspaceContext.ActiveConfiguration,
            Projects = projects
        };
    }

    private static IEnumerable<Project> EnumerateProjects(Projects projects)
    {
        if (projects == null)
        {
            yield break;
        }

        foreach (var project in projects.Cast<Project>().Where(project => project != null))
        {
            if (IsSolutionFolder(project))
            {
                foreach (var nested in EnumerateProjectItems(project.ProjectItems))
                {
                    yield return nested;
                }

                continue;
            }

            yield return project;
        }
    }

    private static IEnumerable<Project> EnumerateProjectItems(ProjectItems items)
    {
        if (items == null)
        {
            yield break;
        }

        foreach (ProjectItem item in items)
        {
            Project subProject = null;

            try
            {
                subProject = item.SubProject;
            }
            catch
            {
                // ignored
            }

            if (subProject == null)
            {
                continue;
            }

            if (IsSolutionFolder(subProject))
            {
                foreach (var nested in EnumerateProjectItems(subProject.ProjectItems))
                {
                    yield return nested;
                }

                continue;
            }

            yield return subProject;
        }
    }

    private ProjectContextItem CreateProjectContextItem(Project project)
    {
        if (string.IsNullOrWhiteSpace(project.FullName))
        {
            return null;
        }

        return new ProjectContextItem
        {
            Name = project.Name,
            FullPath = project.FullName,
            RelativePath = GetRelativePath(project.FullName),

            TargetFramework =
                GetPropertyValue(project, "FriendlyTargetFramework") ??
                GetPropertyValue(project, "TargetFrameworkMoniker") ??
                GetPropertyValue(project, "TargetFrameworkMonikers"),

            OutputType = GetOutputType(project),

            AssemblyName =
                GetPropertyValue(project, "AssemblyName"),

            RootNamespace =
                GetPropertyValue(project, "RootNamespace") ??
                GetPropertyValue(project, "DefaultNamespace")
        };
    }

    private static string GetPropertyValue(Project project, params string[] names)
    {
        if (project?.Properties == null)
        {
            return null;
        }

        foreach (var name in names)
        {
            try
            {
                var property = project.Properties.Item(name);

                if (property?.Value != null)
                {
                    return property.Value.ToString();
                }
            }
            catch
            {
                // ignored
            }
        }

        return null;
    }

    private string GetRelativePath(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return null;
        }

        var root = workspaceContext.SolutionDirectory;

        if (string.IsNullOrWhiteSpace(root))
        {
            return fullPath;
        }

        try
        {
            if (!root.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                root += Path.DirectorySeparatorChar;
            }

            var rootUri = new Uri(root);
            var fileUri = new Uri(fullPath);

            return Uri.UnescapeDataString(
                rootUri.MakeRelativeUri(fileUri)
                    .ToString()
                    .Replace('/', Path.DirectorySeparatorChar));
        }
        catch
        {
            return fullPath;
        }
    }
    private static string GetOutputType(Project project)
    {
        var value =
            GetPropertyValue(project, "OutputTypeEx") ??
            GetPropertyValue(project, "OutputType");

        return value switch
        {
            "1" => "Executable",
            "2" => "Library",
            "3" => "Windows Executable",
            _ => value
        };
    }
    private static bool IsSolutionFolder(Project project)
    {
        return string.Equals(
            project.Kind,
            SolutionFolderKind,
            StringComparison.OrdinalIgnoreCase);
    }
}

#pragma warning restore VSTHRD010
