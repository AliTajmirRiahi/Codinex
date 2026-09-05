using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Services;
using Codinex.Storage.Managers;

namespace Codinex.VisualStudio.Notifications;

/// <summary>
/// Shows <see cref="ToastWindow"/> popups stacked above the system tray clock, only when
/// Visual Studio is in the background (not focused, or minimized) and the feature is enabled.
/// Clicking a toast brings the host window to front; it also auto-dismisses after the
/// configured delay.
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
public sealed class ToastNotificationService(
    IHostWindowStateProvider hostWindowState,
    SettingsManager settingsManager,
    IUiThreadDispatcher uiThreadDispatcher) : IToastNotificationService, IDisposable
{
    private const double ScreenMargin = 12;
    private const double StackGap = 8;
    private const int SwRestore = 9;

    private readonly List<ToastWindow> _open = [];

    public async Task ShowAsync(string title, string message)
    {
        var settings = settingsManager.Settings;

        if (settings is not { EnableBackgroundToast: true })
        {
            return;
        }

        if (hostWindowState.IsHostFocused && !hostWindowState.IsHostMinimized)
        {
            // Visual Studio already has the user's attention - a popup would just be noise.
            return;
        }

        await uiThreadDispatcher.SwitchToMainThreadAsync();

        var toast = new ToastWindow(title, message);

        var seconds = Math.Max(1, settings.ToastAutoDismissSeconds);
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };

        toast.ToastClicked += (_, _) =>
        {
            BringHostToFront();
            toast.Close();
        };

        toast.Closed += (_, _) =>
        {
            // Stop the timer regardless of what closed the toast (the × button, a click,
            // or the timer itself) so it never fires Close() on an already-closed window.
            timer.Stop();
            _open.Remove(toast);
            RepositionAll();
        };

        _open.Add(toast);

        // ShowActivated=false on ToastWindow keeps this from stealing focus.
        toast.Show();
        RepositionAll();

        timer.Tick += (_, _) => toast.Close();
        timer.Start();
    }

    public void Dispose()
    {
        foreach (var toast in _open.ToArray())
        {
            toast.Close();
        }
    }

    // TODO: multi-monitor - this always targets the primary monitor's work area.
    private void RepositionAll()
    {
        var workArea = SystemParameters.WorkArea;
        var bottom = workArea.Bottom - ScreenMargin;

        // Newest toast lands at the bottom; older ones stack upward above it.
        for (var i = _open.Count - 1; i >= 0; i--)
        {
            var toast = _open[i];

            toast.Left = workArea.Right - toast.ActualWidth - ScreenMargin;
            toast.Top = bottom - toast.ActualHeight;

            bottom -= toast.ActualHeight + StackGap;
        }
    }

    private static void BringHostToFront()
    {
        var handle = Process.GetCurrentProcess().MainWindowHandle;

        if (handle == IntPtr.Zero)
        {
            return;
        }

        if (IsIconic(handle))
        {
            ShowWindow(handle, SwRestore);
        }

        SetForegroundWindow(handle);
    }

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
