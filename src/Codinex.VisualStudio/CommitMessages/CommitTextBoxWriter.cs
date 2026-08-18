using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;

namespace Codinex.VisualStudio.CommitMessages
{
    /// <summary>
    /// Writes text into the native Git Changes commit textbox found by
    /// <see cref="GitCommitVisualTreeLocator"/>. Must be called on the UI thread.
    /// </summary>
    internal static class CommitTextBoxWriter
    {
        public static bool TryWrite(FrameworkElement textBox, string text)
        {
            if (textBox == null) return false;

            // Paste first: a real Ctrl+V goes through the textbox's own paste handling, which
            // correctly preserves embedded newlines for multi-line commit messages. VS's private
            // LabeledTextBox automation-peer implementation of IValueProvider.SetValue strips
            // embedded newlines (title and body collapse onto one line with no separator at
            // all), so ValuePattern is only used as a last-resort fallback below.
            if (TryWriteViaKeyboard(textBox, text))
            {
                return true;
            }

            try
            {
                var peer = UIElementAutomationPeer.CreatePeerForElement(textBox)
                           ?? FrameworkElementAutomationPeer.CreatePeerForElement(textBox);

                if (peer?.GetPattern(PatternInterface.Value) is IValueProvider valuePattern
                    && !valuePattern.IsReadOnly)
                {
                    valuePattern.SetValue(text ?? string.Empty);
                    return true;
                }
            }
            catch
            {
                // give up — never let a UI write crash VS
            }

            return false;
        }

        private static bool TryWriteViaKeyboard(FrameworkElement textBox, string text)
        {
            try
            {
                textBox.Focus();
                Keyboard.Focus(textBox);

                var previousClipboard = TryGetClipboardText();

                Clipboard.SetText(text ?? string.Empty);

                NativeMethods.SendCtrlA();
                Thread.Sleep(30);
                NativeMethods.SendCtrlV();

                if (previousClipboard != null)
                {
                    // SendCtrlV() only queues the keystroke (via keybd_event) — it does not
                    // process it. We're still running synchronously on the UI thread, so the
                    // actual paste hasn't happened yet. Restoring the clipboard here races the
                    // paste and silently pastes the OLD clipboard content instead of ours.
                    // Defer the restore to ApplicationIdle so it runs only after the input
                    // queue (including our injected keys) has actually been processed.
                    textBox.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.ApplicationIdle,
                        new System.Action(() =>
                        {
                            try { Clipboard.SetText(previousClipboard); } catch { /* ignore */ }
                        }));
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Locks/unlocks the textbox for editing without disabling it (a disabled control looks
        /// greyed out and won't accept a still-pending paste). VS's private LabeledTextBox has
        /// its own public IsReadOnly property (confirmed via decompiled XAML — it's already
        /// data-bound to IsCommitMessageReadOnly natively), found here by reflection since we
        /// don't compile against that private assembly. Falls back to IsEnabled if the control
        /// doesn't expose IsReadOnly.
        /// </summary>
        public static void SetReadOnly(FrameworkElement textBox, bool readOnly)
        {
            if (textBox == null) return;

            try
            {
                var property = textBox.GetType().GetProperty("IsReadOnly");

                if (property != null && property.CanWrite && property.PropertyType == typeof(bool))
                {
                    property.SetValue(textBox, readOnly);
                    return;
                }
            }
            catch
            {
                // fall through to the IsEnabled fallback
            }

            textBox.IsEnabled = !readOnly;
        }

        private static string TryGetClipboardText()
        {
            try
            {
                return Clipboard.ContainsText() ? Clipboard.GetText() : null;
            }
            catch
            {
                return null;
            }
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, System.UIntPtr dwExtraInfo);

            private const byte VK_CONTROL = 0x11;
            private const byte VK_A = 0x41;
            private const byte VK_V = 0x56;
            private const uint KEYEVENTF_KEYUP = 0x0002;

            public static void SendCtrlA()
            {
                keybd_event(VK_CONTROL, 0, 0, System.UIntPtr.Zero);
                keybd_event(VK_A, 0, 0, System.UIntPtr.Zero);
                keybd_event(VK_A, 0, KEYEVENTF_KEYUP, System.UIntPtr.Zero);
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, System.UIntPtr.Zero);
            }

            public static void SendCtrlV()
            {
                keybd_event(VK_CONTROL, 0, 0, System.UIntPtr.Zero);
                keybd_event(VK_V, 0, 0, System.UIntPtr.Zero);
                keybd_event(VK_V, 0, KEYEVENTF_KEYUP, System.UIntPtr.Zero);
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, System.UIntPtr.Zero);
            }
        }
    }
}
