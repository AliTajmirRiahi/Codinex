namespace Codify.Core.Interfaces
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
    }
}