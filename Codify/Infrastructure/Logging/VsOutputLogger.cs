using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace Codify.Infrastructure.Logging
{
    /// <summary>
    /// Writes logs to a Visual Studio output pane.
    /// </summary>
    public sealed class VsOutputLogger : IVsOutputLogger
    {
        private readonly IVsOutputWindowPane _pane;

        public VsOutputLogger(IVsOutputWindowPane pane)
        {
            _pane = pane ?? throw new ArgumentNullException(nameof(pane));
        }

        public void WriteLine(string message)
        {
            // Output window calls should be thread-safe in VS extensions.
            _pane.OutputStringThreadSafe(message + Environment.NewLine);
        }
    }
}