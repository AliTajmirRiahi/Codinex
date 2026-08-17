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
                // fall through to the keyboard/clipboard fallback
            }

            return TryWriteViaKeyboard(textBox, text);
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
                    // Best-effort restore; never let clipboard cleanup fail the write.
                    try { Clipboard.SetText(previousClipboard); } catch { /* ignore */ }
                }

                return true;
            }
            catch
            {
                return false;
            }
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
