using System;
using System.Threading.Tasks;

namespace Codinex.Core.Interfaces.Services
{
    /// <summary>
    /// Single owner of the zoom level of the Codinex web UI. Ctrl + mouse wheel and the
    /// zoom field in Codinex Settings both go through here, so every open tool window
    /// shows the same scale and the level survives a restart.
    /// </summary>
    public interface IUiZoomService
    {
        // Lowest zoom the UI can be taken to, in percent.
        int MinPercent { get; }

        // Highest zoom the UI can be taken to, in percent.
        int MaxPercent { get; }

        // Current zoom level, in percent, already clamped to the supported range.
        int ZoomPercent { get; }

        // Raised after a new zoom level has been persisted, so open tool windows can follow.
        event EventHandler<int> ZoomPercentChanged;

        // Clamps, persists and broadcasts a new zoom level.
        Task SetZoomPercentAsync(int percent);

        // Clamps a raw percentage to the supported range.
        int Clamp(int percent);
    }
}
