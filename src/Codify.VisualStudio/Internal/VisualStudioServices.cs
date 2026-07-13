using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading.Tasks;
using Codify.VisualStudio.Interfaces;
using Microsoft.CodeAnalysis;

namespace Codify.VisualStudio.Internal
{
    /// <summary>
    /// Default implementation for accessing Visual Studio services.
    /// </summary>
    public sealed class VisualStudioServices(IAsyncServiceProvider serviceProvider) : IVisualStudioServices
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
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            return await serviceProvider.GetServiceAsync(typeof(SDTE)) as DTE2;
        }

        public async Task<IVsSolution> GetSolutionAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            return await serviceProvider.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
        }

        public async Task<IVsOutputWindow> GetOutputWindowAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            return await serviceProvider.GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
        }

        public async Task<IVsUIShell> GetUiShellAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            return await serviceProvider.GetServiceAsync(typeof(SVsUIShell)) as IVsUIShell;
        }

        public async Task<Workspace> GetWorkspaceAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var componentModel =
                await GetServiceAsync<SComponentModel>() as IComponentModel;

            return componentModel?
                .GetService<Workspace>();
        }
    }
}