using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codify.Core.Abstractions
{
    public interface IThemeService
    {
        // Get all variable in one string
        string GetCurrentThemeAsCssVariables();

        // Event for UI Update
        event EventHandler ThemeChanged;
    }
}
