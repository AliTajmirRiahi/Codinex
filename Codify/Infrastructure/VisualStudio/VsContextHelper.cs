using System;
using EnvDTE;
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
        private const string _defaultProjectName = "DefaultProject";
        /// <summary>
        /// Gets the name of the project associated with the currently active document.
        /// </summary>
        public static string GetActiveProjectName()
        {
            try
            {
                // Thread safety check: DTE must be accessed on the UI thread.
                ThreadHelper.ThrowIfNotOnUIThread();

                // Get the DTE service from the global service provider
                var dte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

                if (dte == null) return _defaultProjectName;

                // 1. Try to get the project from the active document (best for context)
                if (dte.ActiveDocument?.ProjectItem?.ContainingProject != null)
                {
                    return dte.ActiveDocument.ProjectItem.ContainingProject.Name;
                }

                // 2. Fallback: Try to get the selected project in Solution Explorer
                if (dte.SelectedItems != null && dte.SelectedItems.Count > 0)
                {
                    foreach (SelectedItem item in dte.SelectedItems)
                    {
                        if (item.Project != null) return item.Project.Name;
                        if (item.ProjectItem?.ContainingProject != null) return item.ProjectItem.ContainingProject.Name;
                    }
                }

                return _defaultProjectName;
            }
            catch (Exception ex)
            {
                // Log exception if needed (System.Diagnostics.Debug.WriteLine(ex.Message);)
                return _defaultProjectName;
            }
        }
    }
}