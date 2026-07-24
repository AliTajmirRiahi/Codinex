using Codify.Core.Interfaces;
using Codify.VisualStudio.Interfaces;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading.Tasks;
#pragma warning disable VSTHRD010

namespace Codify.VisualStudio.Internal
{
    /// <summary>
    /// Default implementation for accessing Visual Studio services.
    /// </summary>
    public sealed class VisualStudioServices(IAsyncServiceProvider serviceProvider, IUiThreadDispatcher uiThreadDispatcher) : IVisualStudioServices
    {
        public async Task<T> GetServiceAsync<T>()
            where T : class
        {
            if (typeof(T) == typeof(DTE2))
                return await GetDteAsync() as T;

            return await serviceProvider.GetServiceAsync(typeof(T)) as T;
        }

        public Task<object> GetServiceAsync(Type serviceType)
        {
            return serviceProvider.GetServiceAsync(serviceType == typeof(DTE2) ? typeof(SDTE) : serviceType);
        }

        public async Task<DTE2> GetDteAsync()
        {
            await uiThreadDispatcher.SwitchToMainThreadAsync();

            return await serviceProvider.GetServiceAsync(typeof(SDTE)) as DTE2;
        }

        public async Task<IVsSolution> GetSolutionAsync()
        {
            await uiThreadDispatcher.SwitchToMainThreadAsync();

            return await serviceProvider.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
        }

        public async Task<IVsOutputWindow> GetOutputWindowAsync()
        {
            await uiThreadDispatcher.SwitchToMainThreadAsync();

            return await serviceProvider.GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
        }

        public async Task<IVsUIShell> GetUiShellAsync()
        {
            await uiThreadDispatcher.SwitchToMainThreadAsync();

            return await serviceProvider.GetServiceAsync(typeof(SVsUIShell)) as IVsUIShell;
        }

        public async Task<VisualStudioWorkspace> GetWorkspaceAsync()
        {
            await uiThreadDispatcher.SwitchToMainThreadAsync();

            var componentModel =
                await GetServiceAsync<SComponentModel>() as IComponentModel;

            return componentModel?
                .GetService<VisualStudioWorkspace>();
        }

        public async Task<IVsSolutionBuildManager> GetSolutionBuildManagerAsync()
        {
            await uiThreadDispatcher.SwitchToMainThreadAsync();

            return await serviceProvider.GetServiceAsync(typeof(SVsSolutionBuildManager))
                as IVsSolutionBuildManager;
        }
    }
}
#pragma warning restore VSTHRD010
