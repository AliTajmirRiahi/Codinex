using System;

namespace Codify.Infrastructure.Errors
{
    /// <summary>
    /// Centralized error handling contract.
    /// All exceptions should be routed here before being exposed to the UI.
    /// </summary>
    public interface IErrorHandler
    {
        /// <summary>
        /// Handles an exception from a given source and writes full diagnostics to output.
        /// </summary>
        void Handle(Exception exception, string source, object? context = null);

        /// <summary>
        /// Builds a safe user-facing message that can be shown in Chat/UI.
        /// </summary>
        string GetUserFacingMessage();
    }
}