namespace Codinex.Core.Interfaces.Services
{
    /// <summary>
    /// Shows safe user-facing notifications.
    /// This service must never display technical exception details to the user.
    /// </summary>
    public interface IUserNotificationService
    {
        /// <summary>
        /// Shows a safe error message to the user.
        /// </summary>
        void ShowError(string message);

        /// <summary>
        /// Shows an informational message to the user (not an error).
        /// </summary>
        void ShowInfo(string message);
    }
}