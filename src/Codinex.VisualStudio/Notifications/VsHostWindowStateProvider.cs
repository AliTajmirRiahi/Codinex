using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Services;

namespace Codinex.VisualStudio.Notifications;

/// <summary>
/// Determines focus/minimized state for the Visual Studio host process via Win32, since there
/// is no VS SDK API for "is my own main window in the foreground".
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Foundation)]
public sealed class VsHostWindowStateProvider : IHostWindowStateProvider
{
    public bool IsHostFocused
    {
        get
        {
            var foreground = GetForegroundWindow();

            if (foreground == IntPtr.Zero)
            {
                return false;
            }

            GetWindowThreadProcessId(foreground, out var foregroundProcessId);

            return foregroundProcessId == (uint)Process.GetCurrentProcess().Id;
        }
    }

    public bool IsHostMinimized
    {
        get
        {
            var mainWindow = Process.GetCurrentProcess().MainWindowHandle;

            return mainWindow != IntPtr.Zero && IsIconic(mainWindow);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);
}
