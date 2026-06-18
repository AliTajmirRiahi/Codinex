using System;
using Codify.Core.Abstractions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Codify.Infrastructure.VisualStudio
{
    /// <summary>
    /// Displays Visual Studio native message boxes.
    /// This class is intended for startup/bootstrap errors only.
    /// </summary>
    public sealed class VsUserNotificationService : IUserNotificationService
    {
        private readonly AsyncPackage _package;

        /// <summary>
        /// Creates a Visual Studio user notification service.
        /// </summary>
        public VsUserNotificationService(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
        }

        /// <summary>
        /// Shows a safe error message to the user.
        /// Technical error details must be written to the Output window, not shown here.
        /// </summary>
        public void ShowError(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            VsShellUtilities.ShowMessageBox(
                _package,
                message,
                "Codify AI",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}