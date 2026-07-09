using EnvDTE80;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading.Tasks;

namespace Codify.Infrastructure.VisualStudio.Internal
{
    /// <summary>
    /// Provides access to Visual Studio services.
    /// Encapsulates service resolution and hides VS SDK implementation details.
    /// </summary>
    public interface IVisualStudioServices
    {
        /// <summary>
        /// Gets any Visual Studio service.
        /// </summary>
        Task<T> GetServiceAsync<T>()
            where T : class;

        /// <summary>
        /// Gets any Visual Studio service by type.
        /// </summary>
        Task<object> GetServiceAsync(Type serviceType);

        /// <summary>
        /// Gets the DTE automation object.
        /// </summary>
        Task<DTE2> GetDteAsync();

        /// <summary>
        /// Gets the active solution service.
        /// </summary>
        Task<IVsSolution> GetSolutionAsync();

        /// <summary>
        /// Gets the Output Window service.
        /// </summary>
        Task<IVsOutputWindow> GetOutputWindowAsync();

        /// <summary>
        /// Gets the Visual Studio shell.
        /// </summary>
        Task<IVsUIShell> GetUiShellAsync();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<VisualStudioWorkspace> GetWorkspaceAsync();
    }
}