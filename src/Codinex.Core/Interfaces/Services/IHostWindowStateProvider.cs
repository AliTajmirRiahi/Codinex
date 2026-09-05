namespace Codinex.Core.Interfaces.Services
{
    /// <summary>
    /// Reports whether the host application (Visual Studio) currently has the user's
    /// attention, so background notifications can be shown only when they would otherwise
    /// go unnoticed.
    /// </summary>
    public interface IHostWindowStateProvider
    {
        /// <summary>True when the foreground window belongs to this process.</summary>
        bool IsHostFocused { get; }

        /// <summary>True when the host's main window is minimized.</summary>
        bool IsHostMinimized { get; }
    }
}
