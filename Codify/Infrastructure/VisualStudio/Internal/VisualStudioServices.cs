using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading.Tasks;

namespace Codify.Infrastructure.VisualStudio.Internal
{
    /// <summary>
    /// Default implementation for accessing Visual Studio services.
    /// </summary>
    public sealed class VisualStudioServices : IVisualStudioServices
    {
        private readonly IAsyncServiceProvider _serviceProvider;

        public VisualStudioServices(IAsyncServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<T> GetServiceAsync<T>()
            where T : class
        {
            if (typeof(T) == typeof(DTE2))
                return await GetDteAsync() as T;

            return await _serviceProvider.GetServiceAsync(typeof(T)) as T;
        }

        public Task<object> GetServiceAsync(Type serviceType)
        {
            return _serviceProvider.GetServiceAsync(serviceType == typeof(DTE2) ? typeof(SDTE) : serviceType);
        }

        public async Task<DTE2> GetDteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            return await _serviceProvider.GetServiceAsync(typeof(SDTE)) as DTE2;
        }

        public async Task<IVsSolution> GetSolutionAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            return await _serviceProvider.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
        }

        public async Task<IVsOutputWindow> GetOutputWindowAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            return await _serviceProvider.GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
        }

        public async Task<IVsUIShell> GetUiShellAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            return await _serviceProvider.GetServiceAsync(typeof(SVsUIShell)) as IVsUIShell;
        }

        public async Task<VisualStudioWorkspace> GetWorkspaceAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var componentModel =
                await GetServiceAsync<SComponentModel>() as IComponentModel;

            return componentModel?
                .GetService<VisualStudioWorkspace>();
        }
    }
}