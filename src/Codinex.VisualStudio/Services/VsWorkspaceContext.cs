using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Codify.VisualStudio.Services
{
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Foundation)]
    public sealed class VsWorkspaceContext : IWorkspaceContext
    {
        private const string DefaultSolutionName = "DefaultSolution";

        public string SolutionName
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (Package.GetGlobalService(typeof(SDTE)) is not DTE2 dte)
                    return DefaultSolutionName;

                return string.IsNullOrWhiteSpace(dte.Solution?.FullName)
                    ? DefaultSolutionName
                    : Path.GetFileNameWithoutExtension(dte.Solution.FullName);
            }
        }

        public string SolutionPath
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (Package.GetGlobalService(typeof(SDTE)) is not DTE2 dte)
                    return string.Empty;

                return dte.Solution?.FullName ?? string.Empty;
            }
        }

        public string SolutionDirectory
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (Package.GetGlobalService(typeof(SDTE)) is not DTE2 dte)
                    return string.Empty;

                var solution = dte.Solution?.FullName;

                if (string.IsNullOrWhiteSpace(solution))
                    return string.Empty;

                return Path.GetDirectoryName(solution) ?? string.Empty;
            }
        }


        public bool IsSolutionOpen
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (Package.GetGlobalService(typeof(SDTE)) is not DTE2 dte)
                    return false;

                return dte.Solution?.IsOpen == true;
            }
        }

        public string ActiveProjectName
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (Package.GetGlobalService(typeof(SDTE)) is not DTE2 dte)
                    return string.Empty;

                return dte.ActiveDocument?.ProjectItem?.ContainingProject?.Name ?? string.Empty;
            }
        }

        public string ActiveDocumentPath
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (Package.GetGlobalService(typeof(SDTE)) is not DTE2 dte)
                    return string.Empty;

                return dte.ActiveDocument?.FullName ?? string.Empty;
            }
        }

        public IReadOnlyList<string> StartupProjects
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (Package.GetGlobalService(typeof(SDTE)) is not DTE2 dte || dte.Solution?.SolutionBuild?.StartupProjects is not Array startupProjects || startupProjects.Length == 0)
                    return [];

                return startupProjects
                    .Cast<object>()
                    .Select(x => x?.ToString())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();
            }
        }

        public string ActiveConfiguration
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (Package.GetGlobalService(typeof(SDTE)) is not DTE2 dte)
                    return string.Empty;

                return dte.Solution?.SolutionBuild?.ActiveConfiguration?.Name
                       ?? string.Empty;
            }
        }

    }
}