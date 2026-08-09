using System;

namespace Codinex.Core.Models
{
    public sealed class InputLanguageChangedEventArgs : EventArgs
    {
        /// <summary>
        /// BCP-47 / culture tag of the new input language (e.g. "fa-IR", "en-US").
        /// </summary>
        public string LanguageTag { get; set; }

        /// <summary>
        /// Human readable name of the new input language (e.g. "Persian (Iran)").
        /// </summary>
        public string LanguageName { get; set; }

        /// <summary>
        /// True when the new input language reads right-to-left (e.g. Farsi, Arabic, Hebrew).
        /// </summary>
        public bool IsRightToLeft { get; set; }
    }
}
