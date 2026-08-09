using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;

namespace Codinex.VisualStudio.Input
{
    /// <summary>
    /// Listens for the current Windows keyboard input language changing (e.g. the user
    /// switching the language bar between English and Farsi) and republishes it as a
    /// framework-agnostic event so the WebView UI can react (e.g. flip the composer to RTL).
    /// </summary>
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Infrastructure)]
    public sealed class VsInputLanguageWatcher(IUiThreadDispatcher uiThreadDispatcher)
        : IInputLanguageWatcher, IStartupTask, IDisposable
    {
        private bool _isSubscribed;

        public event EventHandler<Codinex.Core.Models.InputLanguageChangedEventArgs> InputLanguageChanged;

        public async Task StartAsync()
        {
            await uiThreadDispatcher.SwitchToMainThreadAsync();

            if (_isSubscribed) return;

            InputLanguageManager.Current.InputLanguageChanged += OnInputLanguageChanged;
            _isSubscribed = true;

            // Publish the language that's active right now so the UI can sync on startup,
            // without waiting for the user to actually switch languages.
            RaiseChanged(InputLanguageManager.Current.CurrentInputLanguage);
        }

        private void OnInputLanguageChanged(object sender, InputLanguageEventArgs e)
        {
            RaiseChanged(e.NewLanguage);
        }

        private void RaiseChanged(CultureInfo culture)
        {
            if (culture == null) return;

            InputLanguageChanged?.Invoke(this, new Codinex.Core.Models.InputLanguageChangedEventArgs
            {
                LanguageTag = culture.Name,
                LanguageName = culture.EnglishName,
                IsRightToLeft = culture.TextInfo.IsRightToLeft
            });
        }

        public void Dispose()
        {
            if (!_isSubscribed) return;

            InputLanguageManager.Current.InputLanguageChanged -= OnInputLanguageChanged;
            _isSubscribed = false;
        }
    }
}
