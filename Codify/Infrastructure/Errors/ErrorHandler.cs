using Codify.Core.Abstractions;
using Codify.Infrastructure.Logging;
using System;

namespace Codify.Infrastructure.Errors
{
    /// <summary>
    /// Centralized error handler that logs full details to Visual Studio output
    /// and returns only a safe generic message to the user.
    /// </summary>
    public sealed class ErrorHandler : IErrorHandler
    {
        private readonly IVsOutputLogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        public ErrorHandler(IVsOutputLogger logger, IJsonSerializer jsonSerializer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonSerializer = jsonSerializer;
        }

        public void Handle(Exception exception, string source, object? context = null)
        {
            var error = new ErrorInfo
            {
                Source = source,
                Message = exception.Message,
                StackTrace = exception.ToString(),
                Context = context is null ? null : _jsonSerializer.Serialize(context)
            };

            // Full diagnostic log only goes to output window.
            _logger.WriteLine(
                $"[ERROR] [{error.TimestampUtc:O}] " +
                $"Id={error.ErrorId} Source={error.Source} Message={error.Message} " +
                $"Context={error.Context}\n{error.StackTrace}"
            );
        }

        public string GetUserFacingMessage()
        {
            return ErrorMessages.GenericChatError;
        }
    }
}