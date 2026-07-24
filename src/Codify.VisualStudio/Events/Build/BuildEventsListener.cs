using System;
using System.Threading.Tasks;
using Codify.Core.Interfaces;
using Codify.VisualStudio.Interfaces;
using Codify.VisualStudio.Internal;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

#pragma warning disable VSTHRD010

namespace Codify.VisualStudio.Events.Build
{
    /// <summary>
    /// Bridges Visual Studio build events to the application event model.
    /// </summary>
    public sealed class BuildEventsListener(
        IVisualStudioServices visualStudio,
        IUiThreadDispatcher uiThreadDispatcher)
        :
            VsServiceBase(visualStudio),
            IVsUpdateSolutionEvents2,
            IBuildEvents,
            IDisposable
    {
        private readonly IUiThreadDispatcher _uiThreadDispatcher = uiThreadDispatcher
                                                                   ?? throw new ArgumentNullException(nameof(uiThreadDispatcher));

        private IVsSolutionBuildManager _buildManager;

        private uint _cookie;

        private bool _initialized;

        /// <inheritdoc />
        public event EventHandler BuildStarted;

        /// <inheritdoc />
        public event EventHandler BuildCompleted;

        /// <summary>
        /// Registers for Visual Studio build events.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_initialized)
            {
                return;
            }

            await _uiThreadDispatcher.SwitchToMainThreadAsync();

            _buildManager = await GetSolutionBuildManagerAsync();

            if (_buildManager == null)
            {
                throw new InvalidOperationException(
                    "Unable to resolve IVsSolutionBuildManager.");
            }

            ErrorHandler.ThrowOnFailure(
                _buildManager.AdviseUpdateSolutionEvents(
                    this,
                    out _cookie));

            _initialized = true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_initialized)
            {
                return;
            }

            ErrorHandler.ThrowOnFailure(
                _buildManager.UnadviseUpdateSolutionEvents(_cookie));

            _cookie = 0;
            _initialized = false;
        }

        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            RaiseBuildStarted();

            return VSConstants.S_OK;
        }

        public int UpdateSolution_Done(
            int fSucceeded,
            int fModified,
            int fCancelCommand)
        {
            RaiseBuildCompleted();

            return VSConstants.S_OK;
        }

        public int UpdateProjectCfg_Begin(
            IVsHierarchy pHierProj,
            IVsCfg pCfgProj,
            IVsCfg pCfgSln,
            uint dwAction,
            ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int UpdateProjectCfg_Done(
            IVsHierarchy pHierProj,
            IVsCfg pCfgProj,
            IVsCfg pCfgSln,
            uint dwAction,
            int fSuccess,
            int fCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents2.UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents2.UpdateSolution_Cancel()
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents2.OnActiveProjectCfgChange(
            IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Cancel()
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(
            IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Raises the BuildStarted event.
        /// </summary>
        private void RaiseBuildStarted()
        {
            BuildStarted?.Invoke(
                this,
                EventArgs.Empty);
        }

        /// <summary>
        /// Raises the BuildCompleted event.
        /// </summary>
        private void RaiseBuildCompleted()
        {
            BuildCompleted?.Invoke(
                this,
                EventArgs.Empty);
        }
    }
}

#pragma warning restore VSTHRD010