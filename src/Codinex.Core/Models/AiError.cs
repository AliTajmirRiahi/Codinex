using System;
using Newtonsoft.Json;

namespace Codinex.Core.Models
{
    /// <summary>
    /// Provider-agnostic error details for expected AI provider failures.
    /// </summary>
    public sealed class AiError
    {
        public AiError()
        {
        }

        public AiError(
            AiErrorCode code,
            string message,
            bool isRetryable,
            TimeSpan? retryAfter = null,
            Exception exception = null)
        {
            Code = code;
            Message = message;
            IsRetryable = isRetryable;
            RetryAfter = retryAfter;
            Exception = exception;
        }

        public AiErrorCode Code { get; set; }

        public string Message { get; set; }

        public bool IsRetryable { get; set; }

        public TimeSpan? RetryAfter { get; set; }

        /// <summary>
        /// Gets or sets the original exception for diagnostics only.
        /// This value is intentionally ignored during serialization.
        /// </summary>
        [JsonIgnore]
        public Exception Exception { get; set; }
    }
}
