using System;
using Codify.Core.Interfaces;
using Codify.VisualStudio.Interfaces;

namespace Codify.VisualStudio.Diagnostics.Errors
{
    /// <summary>
    /// Centralized error handler that logs full details to Visual Studio output
    /// and returns only a safe generic message to the user.
    /// </summary>
    public sealed class ErrorHandler(IVsOutputLogger logger, IJsonSerializer jsonSerializer) : IErrorHandler
    {
        private readonly IVsOutputLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
        }

        public string GetUserFacingMessage()
        {
            return ErrorMessages.GenericChatError;
        }
    }
}