using System;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Services;
using Codinex.Storage.Managers;

namespace Codinex.VisualStudio.Services;

/// <summary>
/// Stores the UI zoom level with the rest of the global settings, so the Ctrl + mouse wheel
/// gesture and the zoom field in Codinex Settings write to the same place, and broadcasts
/// every change to the tool windows that are already open.
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
public sealed class UiZoomService(SettingsManager settingsManager) : IUiZoomService
{
    private const int Minimum = 50;
    private const int Maximum = 200;
    private const int Default = 100;

    public int MinPercent => Minimum;

    public int MaxPercent => Maximum;

    // A settings file written before this setting existed (or hand-edited to 0) falls back
    // to 100% rather than to the minimum.
    public int ZoomPercent
    {
        get
        {
            var stored = settingsManager.Settings.UiZoomPercent;

            return stored <= 0 ? Default : Clamp(stored);
        }
    }

    public event EventHandler<int> ZoomPercentChanged;

    public async Task SetZoomPercentAsync(int percent)
    {
        var clamped = Clamp(percent);

        if (settingsManager.Settings.UiZoomPercent != clamped)
        {
            var settings = settingsManager.Settings;

            settings.UiZoomPercent = clamped;

            await settingsManager.SaveSettingsAsync(settings);
        }

        ZoomPercentChanged?.Invoke(this, clamped);
    }

    public int Clamp(int percent)
    {
        if (percent < Minimum) return Minimum;

        return percent > Maximum ? Maximum : percent;
    }
}
