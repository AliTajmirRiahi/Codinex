using System.Threading.Tasks;

namespace Codinex.Core.Interfaces.Services
{
    /// <summary>
    /// Shows a small, non-activating notification near the system tray clock. Implementations
    /// only actually display it when the host window is in the background (not focused, or
    /// minimized) and the feature is enabled in settings - callers do not need to check either
    /// condition themselves.
    /// </summary>
    public interface IToastNotificationService
    {
        Task ShowAsync(string title, string message);
    }
}
