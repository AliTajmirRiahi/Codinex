using System;
using Codify.VisualStudio.Interfaces;
using Microsoft.VisualStudio.Shell.Interop;

namespace Codify.VisualStudio.Logging
{
    /// <summary>
    /// Writes logs to a Visual Studio output pane.
    /// </summary>
    public sealed class VsOutputLogger(IVsOutputWindowPane pane) : IVsOutputLogger
    {
        private readonly IVsOutputWindowPane _pane = pane ?? throw new ArgumentNullException(nameof(pane));

        public void WriteLine(string message)
        {
            // Output window calls should be thread-safe in VS extensions.
#pragma warning disable VSTHRD010
            _pane.OutputStringThreadSafe(message + Environment.NewLine);
#pragma warning restore VSTHRD010
        }
    }
}