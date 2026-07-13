using EnvDTE80;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading.Tasks;
using Codify.VisualStudio.Interfaces;
using Microsoft.CodeAnalysis;

namespace Codify.VisualStudio.Internal
{
    /// <summary>
    /// Base class for services that interact with the Visual Studio SDK.
    /// Provides common helpers for resolving Visual Studio services.
    /// </summary>
    public abstract class VsServiceBase(IVisualStudioServices visualStudio)
    {
        /// <summary>
        /// Provides access to Visual Studio services.
        /// </summary>
        protected IVisualStudioServices VisualStudio { get; } = visualStudio ?? throw new ArgumentNullException(nameof(visualStudio));

        /// <summary>
        /// Resolves any Visual Studio service.
        /// </summary>
        protected Task<T> GetServiceAsync<T>()
            where T : class
        {
            return VisualStudio.GetServiceAsync<T>();
        }

        /// <summary>
        /// Gets the DTE automation object.
        /// </summary>
        protected Task<DTE2> GetDteAsync()
        {
            return VisualStudio.GetDteAsync();
        }

        /// <summary>
        /// Gets the active solution service.
        /// </summary>
        protected Task<IVsSolution> GetSolutionAsync()
        {
            return VisualStudio.GetSolutionAsync();
        }

        /// <summary>
        /// Gets the Output Window service.
        /// </summary>
        protected Task<IVsOutputWindow> GetOutputWindowAsync()
        {
            return VisualStudio.GetOutputWindowAsync();
        }

        /// <summary>
        /// Gets the Visual Studio shell.
        /// </summary>
        protected Task<IVsUIShell> GetUiShellAsync()
        {
            return VisualStudio.GetUiShellAsync();
        }

        protected Task<Workspace> GetWorkspaceAsync()
        {
            return VisualStudio.GetWorkspaceAsync();
        }
    }
}