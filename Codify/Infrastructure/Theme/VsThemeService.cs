using Codify.Core.Abstractions;
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System.Drawing;

namespace Codify.Infrastructure.Theme
{
    public class VsThemeService : IThemeService, IDisposable
    {
        public event EventHandler ThemeChanged;

        public VsThemeService()
        {
            // Listen to VS Theme changes
            VSColorTheme.ThemeChanged += OnVsThemeChanged;
        }

        private void OnVsThemeChanged(ThemeChangedEventArgs e)
        {
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }

        public string GetCurrentThemeAsCssVariables()
        {
            // Extract colors using VSColorTheme
            var bg = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
            var fg = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey);
            var border = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBorderColorKey);
            var button = VSColorTheme.GetThemedColor(EnvironmentColors.SystemHighlightColorKey);

            // Convert to Hex and format as CSS variables
           return $@"
                document.documentElement.style.setProperty('--vs-background', '{ToHex(bg)}');
                document.documentElement.style.setProperty('--vs-foreground', '{ToHex(fg)}');
                document.documentElement.style.setProperty('--vs-border', '{ToHex(border)}');
                document.documentElement.style.setProperty('--vs-input-bg', '{ToHex(border)}');
                document.documentElement.style.setProperty('--vs-button-bg', '{ToHex(button)}');
                document.documentElement.style.setProperty('--vs-button-hover', '{ToHex(button)}');
            ";
        }

        private string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        public void Dispose()
        {
            VSColorTheme.ThemeChanged -= OnVsThemeChanged;
        }
    }

}
