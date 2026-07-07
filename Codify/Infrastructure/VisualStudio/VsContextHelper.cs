using System.IO;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Codify.Infrastructure.VisualStudio
{
    /// <summary>
    /// Provides helper methods to interact with Visual Studio IDE components.
    /// </summary>
    public static class VsContextHelper
    {
        private const string _defaultSolutionName = "DefaultSolution";
        /// <summary>
        /// Gets the name of the Solution associated with the currently active document.
        /// </summary>
        public static string GetCurrentSolutionName()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (Package.GetGlobalService(typeof(SDTE)) is not DTE2 dte || string.IsNullOrWhiteSpace(dte.Solution?.FullName))
                return _defaultSolutionName;

            return Path.GetFileNameWithoutExtension(dte.Solution.FullName);
        }
    }
}