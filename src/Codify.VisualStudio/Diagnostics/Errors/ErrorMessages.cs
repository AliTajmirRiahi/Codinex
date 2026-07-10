namespace Codify.VisualStudio.Diagnostics.Errors
{
    /// <summary>
    /// Centralized user-facing error messages.
    /// These messages are safe to show in Chat/UI.
    /// </summary>
    public static class ErrorMessages
    {
        /// <summary>
        /// Generic message shown to users when an internal error occurs.
        /// No technical details should be exposed in Chat.
        /// </summary>
        public const string GenericChatError =
            "There is some problem, please check output for details";

        /// <summary>
        /// Generic startup error message shown before WebView is available.
        /// </summary>
        public const string StartupError =
            "Codify AI could not start correctly. Please check Visual Studio Output window for details.";
    }
}