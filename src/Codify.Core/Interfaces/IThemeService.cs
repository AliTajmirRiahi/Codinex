using System;

namespace Codify.Core.Interfaces
{
    public interface IThemeService
    {
        // Get all variable in one string
        string GetCurrentThemeAsCssVariables();

        // Event for UI Update
        event EventHandler ThemeChanged;
    }
}
