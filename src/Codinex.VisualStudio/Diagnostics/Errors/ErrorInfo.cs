using System;

namespace Codify.VisualStudio.Diagnostics.Errors
{
    /// <summary>
    /// Represents a normalized application error for internal diagnostics.
    /// This object is only for logging and troubleshooting.
    /// </summary>
    public sealed class ErrorInfo
    {
        /// <summary>
        /// Unique identifier for tracing a specific error instance.
        /// </summary>
        public string ErrorId { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Logical source of the error, such as a class or subsystem name.
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable internal message for diagnostics.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Exception stack trace for debugging.
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Optional serialized context data.
        /// </summary>
        public string? Context { get; set; }

        /// <summary>
        /// UTC timestamp when the error was captured.
        /// </summary>
        public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
    }
}