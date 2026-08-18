using System;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Codinex.VisualStudio.CommitMessages
{
    /// <summary>
    /// Shows commit-message generation errors as a VS InfoBar docked to the native Git Changes
    /// tool window frame — the same banner surface GitHub Copilot's own "sign in" notice uses in
    /// that window. Unlike the button injection, this uses a documented public VS SDK surface
    /// (IVsInfoBarHost / IVsInfoBarUIFactory via a frame property), not visual-tree walking.
    /// </summary>
    internal static class GitChangesInfoBar
    {
        private static IVsInfoBarUIElement _current;
        private static IVsInfoBarHost _currentHost;

        /// <summary>
        /// Shows an error InfoBar in the Git Changes window. Returns false (and shows nothing)
        /// if the window isn't currently open/found — callers should fall back to their own
        /// inline error UI in that case.
        /// </summary>
        public static bool TryShowError(string message)
        {
            try
            {
                var host = FindGitChangesInfoBarHost();
                if (host == null) return false;

                var factory = Package.GetGlobalService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
                if (factory == null) return false;

                RemoveCurrent();

                var model = new InfoBarModel(
                    message,
                    KnownMonikers.StatusError,
                    isCloseButtonVisible: true);

                var element = factory.CreateInfoBar(model);
                if (element == null) return false;

                host.AddInfoBar(element);
                _current = element;
                _currentHost = host;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void RemoveCurrent()
        {
            if (_current == null || _currentHost == null) return;

            try { _currentHost.RemoveInfoBar(_current); }
            catch { /* ignore — never let cleanup break the new banner */ }

            _current = null;
            _currentHost = null;
        }

        private static IVsInfoBarHost FindGitChangesInfoBarHost()
        {
            if (!(Package.GetGlobalService(typeof(SVsUIShell)) is IVsUIShell uiShell)) return null;

            if (uiShell.GetToolWindowEnum(out var enumFrames) < 0 || enumFrames == null) return null;

            var frames = new IVsWindowFrame[1];

            while (enumFrames.Next(1, frames, out var fetched) >= 0 && fetched == 1)
            {
                var frame = frames[0];
                if (frame == null) continue;

                if (frame.GetProperty((int)__VSFPROPID.VSFPROPID_Caption, out var captionObj) < 0) continue;

                var caption = captionObj as string;
                if (string.IsNullOrEmpty(caption)
                    || caption.IndexOf("Git Changes", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (frame.GetProperty((int)__VSFPROPID7.VSFPROPID_InfoBarHost, out var hostObj) >= 0
                    && hostObj is IVsInfoBarHost host)
                {
                    return host;
                }
            }

            return null;
        }
    }
}
