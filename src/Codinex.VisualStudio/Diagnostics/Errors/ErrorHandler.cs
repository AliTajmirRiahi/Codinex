using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.VisualStudio.Interfaces;

namespace Codinex.VisualStudio.Diagnostics.Errors
{
    /// <summary>
    /// Centralized error handler that logs full details to Visual Studio output,
    /// auto-files a GitHub issue for each distinct error, and returns only a safe
    /// generic message to the user.
    /// </summary>
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Foundation)]
    public sealed class ErrorHandler(
        IVsOutputLogger logger,
        IJsonSerializer jsonSerializer,
        IBugReportService bugReportService,
        IVsDiagnosticsCollector vsDiagnosticsCollector) : IErrorHandler
    {
        // Same exception/error repeating in a tight loop (e.g. on every keystroke)
        // must not file a fresh GitHub issue each time.
        private static readonly TimeSpan ReportCooldown = TimeSpan.FromMinutes(15);

        private readonly IVsOutputLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ConcurrentDictionary<string, DateTime> _recentlyReported = new();

        public void Handle(Exception exception, string source, object context = null)
        {
            var error = new ErrorInfo
            {
                Source = source,
                Message = exception.Message,
                StackTrace = exception.ToString(),
                Context = context is null ? null : jsonSerializer.Serialize(context)
            };

            // Full diagnostic log only goes to output window.
            _logger.WriteLine(
                $"[ERROR] [{error.TimestampUtc:O}] " +
                $"Id={error.ErrorId} Source={error.Source} Message={error.Message} " +
                $"Context={error.Context}\n{error.StackTrace}"
            );

            AutoReport(
                $"backend:{source}|{exception.GetType().FullName}|{exception.Message}",
                $"Unhandled exception in {source}.\n\n```\n{error.StackTrace}\n```\n\nContext: {error.Context ?? "(none)"}");
        }

        /// <summary>
        /// Handles errors reported from the WebView UI.
        /// </summary>
        public void HandleUiError(string source,string type, string message, string stack)
        {
            var logMessage =
                $"[UI ERROR]\n : {type}" +
                $"Source: {source}\n" +
                $"Message: {message}\n" +
                $"Stack: {stack}";

            _logger.WriteLine(logMessage);

            AutoReport(
                $"ui:{source}|{type}|{message}",
                $"Unhandled {type} in WebView UI ({source}).\n\nMessage: {message}\n\n```\n{stack}\n```");
        }

        public string GetUserFacingMessage()
        {
            return ErrorMessages.GenericChatError;
        }

        private void AutoReport(string dedupeKey, string description)
        {
#if DEBUG
            // Don't spam GitHub with issues from local dev/debug sessions.
            return;
#else
            var now = DateTime.UtcNow;

            _recentlyReported.TryGetValue(dedupeKey, out var lastReportedAt);

            if (now - lastReportedAt < ReportCooldown)
            {
                return;
            }

            _recentlyReported[dedupeKey] = now;

            // Fire-and-forget: reporting must never block or fail the caller's error path.
            // Failures are logged to the output pane only, never routed back through
            // Handle()/HandleUiError() to avoid a reporting feedback loop.
            _ = Task.Run(async () =>
            {
                try
                {
                    var outputLog = await vsDiagnosticsCollector.CollectOutputLogAsync(CancellationToken.None);
                    var vsInfo = await vsDiagnosticsCollector.CollectVsInfoAsync();

                    var result = await bugReportService.SubmitAsync(
                        chatId: null,
                        description: $"[Auto-reported] {description}",
                        outputLog: outputLog,
                        vsInfo: vsInfo,
                        cancellationToken: CancellationToken.None);

                    if (!result.Success)
                    {
                        _logger.WriteLine($"[AUTO BUG REPORT] Failed: {result.Message}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteLine($"[AUTO BUG REPORT] Failed: {ex.Message}");
                }
            });
#endif
        }
    }
}