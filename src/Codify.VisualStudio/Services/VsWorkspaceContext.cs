using Codify.Core.Interfaces;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;

namespace Codify.VisualStudio.Services
{
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
    }
}