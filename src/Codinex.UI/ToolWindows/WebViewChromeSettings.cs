using Microsoft.Web.WebView2.Core;

namespace Codinex.UI.ToolWindows
{
    /// <summary>
    /// Shared WebView2 hardening applied to every tool window's browser instance.
    /// </summary>
    internal static class WebViewChromeSettings
    {
        /// <summary>
        /// Disables the browser's own right-click menu and auto-grants clipboard
        /// read permission so the WebView UI's custom context menu (select/copy/cut/paste)
        /// can fully replace it, including programmatic paste.
        /// </summary>
        public static void DisableBrowserChrome(CoreWebView2 coreWebView)
        {
#if RELEASE
            coreWebView.Settings.AreDefaultContextMenusEnabled = false;

            coreWebView.PermissionRequested += (s, e) =>
            {
                if (e.PermissionKind == CoreWebView2PermissionKind.ClipboardRead)
                {
                    e.State = CoreWebView2PermissionState.Allow;
                }
            };
#endif
        }
    }
}
