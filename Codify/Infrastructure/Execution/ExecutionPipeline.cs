using Codify.Core.Abstractions;
using Codify.Infrastructure.Errors;
using Microsoft.VisualStudio.Shell;
using System;
using System.Threading.Tasks;

namespace Codify.Infrastructure.Execution
{
    /// <summary>
    /// Central execution boundary for async operations.
    /// All unhandled exceptions should pass through this pipeline.
    /// </summary>
    public sealed class ExecutionPipeline
    {
        private readonly IErrorHandler _errorHandler;
        private readonly IUserNotificationService _userNotificationService;

        /// <summary>
        /// Creates the execution pipeline.
        /// </summary>
        public ExecutionPipeline(
            IErrorHandler errorHandler,
            IUserNotificationService userNotificationService)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _userNotificationService = userNotificationService ?? throw new ArgumentNullException(nameof(userNotificationService));
        }

        /// <summary>
        /// Runs an async action and routes exceptions to the centralized error handler.
        /// </summary>
        public async Task RunAsync(
            Func<Task> action,
            string source,
            bool showMessageBox = false)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, source);

                if (showMessageBox)
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    _userNotificationService.ShowError(ErrorMessages.StartupError);
                }
            }
        }

        /// <summary>
        /// Runs an async function and returns a fallback value when an exception occurs.
        /// </summary>
        public async Task<T> RunAsync<T>(
            Func<Task<T>> action,
            string source,
            T fallback,
            bool showMessageBox = false)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, source);

                if (!showMessageBox) return fallback;

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                _userNotificationService.ShowError(ErrorMessages.StartupError);

                return fallback;
            }
        }
    }
}