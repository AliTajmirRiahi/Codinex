using System;
using Codinex.Core.Models.Documents;

namespace Codinex.Core.Interfaces.Documents
{
    /// <summary>
    /// Watches for changes to the current Windows keyboard input language
    /// (e.g. the user switching between English and Farsi via the language bar).
    /// </summary>
    public interface IInputLanguageWatcher
    {
        event EventHandler<InputLanguageChangedEventArgs> InputLanguageChanged;
    }
}
