using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Context;
using Codinex.Core.Models;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.Models;

namespace Codinex.VisualStudio.Services;

/// <summary>
/// Provides Visual Studio solution build operations.
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Foundation)]
public sealed class BuildService(IVisualStudioServices visualStudioServices, IBuildContextProvider buildContextProvider, IDiagnosticsProvider diagnosticsProvider) : IBuildService
{
    private const string SolutionFolderKind = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";

    public async Task<BuildResult> BuildSolutionAsync(
        CancellationToken cancellationToken)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var buildManager = await visualStudioServices.GetSolutionBuildManagerAsync();

        if (buildManager is not IVsSolutionBuildManager2 buildManager2)
        {
            return new BuildResult()
            {
                Success = false,
                ErrorCount = 0,
                Summary = $"Unable to build solution.",
                Output = "",
            };
        }

        var hr = buildManager.StartSimpleUpdateSolutionConfiguration(
            (uint)(
                VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD |
                VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_FORCE_UPDATE),
            (uint)VSSOLNBUILDQUERYRESULTS.VSSBQR_OUTOFDATE_QUERY_YES,
            0);

        ErrorHandler.ThrowOnFailure(hr);

        await WaitForBuildAsync(buildManager2, cancellationToken);

        var outputContext = await buildContextProvider.GetContextAsync(cancellationToken);

        var diagnostics =
            await diagnosticsProvider.GetDiagnosticsAsync(DiagnosticsScope.Solution, cancellationToken);

        var errors = diagnostics.Where(p => p.Severity == DiagnosticSeverity.Error);

        var diagnosticItems = errors as DiagnosticItem[] ?? errors.ToArray();

        var success = !diagnosticItems.Any();

        var errorCount = diagnosticItems.Count();

        return new BuildResult
        {
            Success = success,
            ErrorCount = errorCount,
            Errors = diagnosticItems,
            Summary = success
                ? "Build succeeded."
                : $"Build failed with {errorCount} error(s).",
            Output = outputContext.Output,
        };

    }

    public async Task<BuildResult> BuildProjectAsync(
        string projectName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            return new BuildResult
            {
                Success = false,
                ErrorCount = 0,
                Summary = "Project name is required.",
                Output = string.Empty
            };
        }

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await visualStudioServices.GetDteAsync();

        if (dte?.Solution is not { IsOpen: true })
        {
            return new BuildResult
            {
                Success = false,
                ErrorCount = 0,
                Summary = "No solution is open.",
                Output = string.Empty
            };
        }

        var project = FindProjectByName(
            dte.Solution.Projects,
            projectName);

        if (project == null)
        {
            return new BuildResult
            {
                Success = false,
                ErrorCount = 0,
                Summary = $"Project '{projectName}' was not found.",
                Output = string.Empty
            };
        }

        var projectDisplayName = project.Name;
        var projectUniqueName = project.UniqueName;

        if (string.IsNullOrWhiteSpace(projectUniqueName))
        {
            return new BuildResult
            {
                Success = false,
                ErrorCount = 0,
                Summary = $"Unable to build project '{projectDisplayName}'.",
                Output = string.Empty
            };
        }

        var solution = await visualStudioServices.GetSolutionAsync();

        ErrorHandler.ThrowOnFailure(
            solution.GetProjectOfUniqueName(
                projectUniqueName,
                out var hierarchy));

        if (hierarchy == null)
        {
            return new BuildResult
            {
                Success = false,
                ErrorCount = 0,
                Summary = $"Unable to locate project '{projectDisplayName}' in the solution.",
                Output = string.Empty
            };
        }

        var buildManager = await visualStudioServices.GetSolutionBuildManagerAsync();

        if (buildManager is not IVsSolutionBuildManager2 buildManager2)
        {
            return new BuildResult()
            {
                Success = false,
                ErrorCount = 0,
                Summary = $"Unable to build project '{projectDisplayName}'.",
                Output = "",
            };
        }

        var hr = buildManager2.StartSimpleUpdateProjectConfiguration(
            hierarchy,
            null,
            null,
            (uint)(
                VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD |
                VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_FORCE_UPDATE),
            (uint)VSSOLNBUILDQUERYRESULTS.VSSBQR_OUTOFDATE_QUERY_YES,
            0);

        ErrorHandler.ThrowOnFailure(hr);

        await WaitForBuildAsync(buildManager2, cancellationToken);

        var outputContext = await buildContextProvider.GetContextAsync(cancellationToken);

        var diagnostics =
            await diagnosticsProvider.GetDiagnosticsAsync(DiagnosticsScope.Solution, cancellationToken);

        var errors = diagnostics
            .Where(p => p.Severity == DiagnosticSeverity.Error)
            .Where(p => string.Equals(
                p.ProjectName,
                projectDisplayName,
                StringComparison.OrdinalIgnoreCase));

        var diagnosticItems = errors as DiagnosticItem[] ?? errors.ToArray();

        var success = !diagnosticItems.Any();

        var errorCount = diagnosticItems.Count();

        return new BuildResult
        {
            Success = success,
            ErrorCount = errorCount,
            Errors = diagnosticItems,
            Summary = success
                ? $"Project '{projectDisplayName}' build succeeded."
                : $"Project '{projectDisplayName}' build failed with {errorCount} error(s).",
            Output = outputContext.Output,
        };
    }

    private static async Task WaitForBuildAsync(
        IVsSolutionBuildManager2 buildManager,
        CancellationToken cancellationToken)
    {
        ErrorHandler.ThrowOnFailure(
            buildManager.QueryBuildManagerBusy(out var busy));

        while (busy != 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Delay(200, cancellationToken);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            ErrorHandler.ThrowOnFailure(
                buildManager.QueryBuildManagerBusy(out busy));
        }
    }

    private static Project FindProjectByName(
        Projects projects,
        string projectName)
    {
        var normalizedProjectName = projectName.Trim();

        return EnumerateProjects(projects)
            .FirstOrDefault(project => IsProjectNameMatch(
                project,
                normalizedProjectName));
    }

    private static bool IsProjectNameMatch(
        Project project,
        string projectName)
    {
        if (project == null)
        {
            return false;
        }

        try
        {
            if (string.Equals(
                    project.Name,
                    projectName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(
                    Path.GetFileNameWithoutExtension(project.FullName),
                    projectName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(
                project.UniqueName,
                projectName,
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<Project> EnumerateProjects(Projects projects)
    {
        if (projects == null)
        {
            yield break;
        }

        foreach (Project project in projects)
        {
            if (project == null)
            {
                continue;
            }

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

    private static bool IsSolutionFolder(Project project)
    {
        try
        {
            return string.Equals(
                project.Kind,
                SolutionFolderKind,
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}