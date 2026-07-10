using System;

namespace Codify.Core.Interfaces
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
        void Handle(Exception exception, string source, object context = null);

        /// <summary>
        /// Handles errors coming from the WebView UI (JavaScript)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="type"></param>
        /// <param name="message"></param>
        /// <param name="stack"></param>
        void HandleUiError(string source, string type, string message, string stack);

        /// <summary>
        /// Builds a safe user-facing message that can be shown in Chat/UI.
        /// </summary>
        string GetUserFacingMessage();
    }
}