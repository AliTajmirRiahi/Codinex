using System;

namespace Codinex.Core.Interfaces.Services
{
    public interface IThemeService
    {
        // Get all variable in one string
        string GetCurrentThemeAsCssVariables();

        // Event for UI Update
        event EventHandler ThemeChanged;
    }
}
