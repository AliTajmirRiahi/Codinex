using Codify.Core.Abstractions;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Color = System.Drawing.Color;

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
            var sb = new System.Text.StringBuilder();

            // Keep existing variables
            var bg = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
            var fg = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey);
            var border = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBorderColorKey);
            var button = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBorderColorKey);

            sb.AppendLine($"document.documentElement.style.setProperty('--vs-background', '{ToHex(bg)}');");
            sb.AppendLine($"document.documentElement.style.setProperty('--vs-foreground', '{ToHex(fg)}');");
            sb.AppendLine($"document.documentElement.style.setProperty('--vs-border', '{ToHex(border)}');");
            sb.AppendLine($"document.documentElement.style.setProperty('--vs-input-bg', '{ToHex(border)}');");
            sb.AppendLine($"document.documentElement.style.setProperty('--vs-button-bg', '{ToHex(button)}');");
            sb.AppendLine($"document.documentElement.style.setProperty('--vs-button-hover', '{ToHex(button)}');");

            // Dynamic Variables from EnvironmentColors
            // We only iterate over properties that end with "Key" and are of type ThemeResourceKey
            var colorProperties = typeof(EnvironmentColors).GetProperties(BindingFlags.Public | BindingFlags.Static);

            foreach (var prop in colorProperties)
            {
                try
                {
                    // The magic fix: Check if it's a ThemeResourceKey (required by GetThemedColor)
                    if (prop.GetValue(null) is ThemeResourceKey themeKey)
                    {
                        var color = VSColorTheme.GetThemedColor(themeKey);
                        var cssVarName = ToKebabCase(prop.Name.Replace("Key", "")); // Remove 'Key' suffix for cleaner CSS

                        sb.AppendLine($"document.documentElement.style.setProperty('--vs-{cssVarName}', '{ToHex(color)}');");
                    }
                }
                catch
                {
                    // Skip keys that are not compatible or cause issues
                    continue;
                }
            }

            return sb.ToString();
        }
        private string ToKebabCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var result = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]) && i > 0) result.Append("-");
                result.Append(char.ToLowerInvariant(name[i]));
            }
            return result.ToString();
        }

        private IEnumerable<(string Name, object? Value)> GetAllEnvironmentColorKeys()
        {
            var type = typeof(EnvironmentColors);

            var flags = BindingFlags.Public | BindingFlags.Static;

            foreach (var prop in type.GetProperties(flags))
            {
                yield return (prop.Name, prop.GetValue(null));
            }
        }
        private static string ToCssVariableName(string name)
        {
            // Convert PascalCase or mixed names to kebab-case
            var chars = new List<char>();

            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];

                if (char.IsUpper(c) && i > 0)
                {
                    chars.Add('-');
                }

                chars.Add(char.ToLowerInvariant(c));
            }

            return new string(chars.ToArray());
        }

        private string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        public void Dispose()
        {
            VSColorTheme.ThemeChanged -= OnVsThemeChanged;
        }
    }

}
