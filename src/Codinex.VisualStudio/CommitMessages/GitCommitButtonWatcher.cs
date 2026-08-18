using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;

namespace Codinex.VisualStudio.CommitMessages
{
    /// <summary>
    /// Periodically tries to inject the "Generate Commit Message" control into Visual Studio's
    /// native Git Changes window. Polling (rather than a one-time attempt) is needed because the
    /// window is created lazily and can be torn down/recreated (floating, docked elsewhere,
    /// closed and reopened) at any time during the session.
    /// </summary>
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
    public sealed class GitCommitButtonWatcher(
        IUiThreadDispatcher uiThreadDispatcher,
        ICommitMessageGenerator commitMessageGenerator,
        IErrorHandler errorHandler)
        : IStartupTask
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

        private DispatcherTimer _timer;

        public async Task StartAsync()
        {
            await uiThreadDispatcher.SwitchToMainThreadAsync();

            var injector = new GitCommitButtonInjector(commitMessageGenerator, errorHandler);

            GitCommitButtonVisibility.Changed += () =>
            {
                if (GitCommitButtonVisibility.IsCodinexOpen)
                {
                    injector.TryInject();
                }
                else
                {
                    injector.Remove();
                }
            };

            _timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = PollInterval
            };
            _timer.Tick += (s, e) => injector.TryInject();
            _timer.Start();
        }
    }
}
