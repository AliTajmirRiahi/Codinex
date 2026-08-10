using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.Internal;

namespace Codinex.VisualStudio
{
    /// <summary>
    /// Displays Visual Studio native message boxes.
    /// This class is intended for startup/bootstrap errors only.
    /// </summary>
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Infrastructure)]
    public sealed class VsUserNotificationService(IVisualStudioServices visualStudioServices, IUiThreadDispatcher uiThreadDispatcher) : VsServiceBase(visualStudioServices), IUserNotificationService
    {

        /// <summary>
        /// Shows a safe error message to the user.
        /// Technical error details must be written to the Output window, not shown here.
        /// </summary>
        public void ShowError(string message)
        {
            _ = ShowErrorAsync(message);
        }

        public async Task ShowErrorAsync(string message)
        {
            await uiThreadDispatcher.SwitchToMainThreadAsync();

            VsShellUtilities.ShowMessageBox(
                VisualStudio.Provider as IServiceProvider ?? throw new InvalidOperationException("VisualStudio => Provider is null"),
                message,
                "Codinex AI",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        /// <summary>
        /// Shows an informational message to the user.
        /// </summary>
        public void ShowInfo(string message)
        {
            _ = ShowInfoAsync(message);
        }

        public async Task ShowInfoAsync(string message)
        {
            await uiThreadDispatcher.SwitchToMainThreadAsync();

            VsShellUtilities.ShowMessageBox(
                VisualStudio.Provider as IServiceProvider ?? throw new InvalidOperationException("VisualStudio => Provider is null"),
                message,
                "Codinex AI",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}