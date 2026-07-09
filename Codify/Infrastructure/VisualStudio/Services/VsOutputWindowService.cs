using Codify.Infrastructure.VisualStudio.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Codify.Infrastructure.VisualStudio.Internal;

namespace Codify.Infrastructure.VisualStudio.Services
{
    /// <summary>
    /// Reads information from the Visual Studio Output window.
    /// </summary>
    public sealed class VsOutputWindowService : VsServiceBase , IVsOutputWindowService
    {
        public VsOutputWindowService(IVisualStudioServices visualStudio) : base(visualStudio)
        {
        }

        public async Task<IReadOnlyList<OutputPaneInfo>> GetOutputPanesAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var result = new List<OutputPaneInfo>();

            if (await GetDteAsync() is not { } dte)
                return result;

            var panes = dte.ToolWindows.OutputWindow.OutputWindowPanes;

            result.AddRange(from OutputWindowPane pane in panes select new OutputPaneInfo { Name = pane.Name });

            return result;
        }

        public async Task<string> ReadOutputAsync(string paneName)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (await GetDteAsync() is not { } dte)
                return string.Empty;

            var pane = dte.ToolWindows.OutputWindow.OutputWindowPanes.Cast<OutputWindowPane>().FirstOrDefault(item => String.Equals(item.Name, paneName, StringComparison.OrdinalIgnoreCase));

            if (pane == null)
                return string.Empty;

            //
            // TODO:
            // EnvDTE does NOT expose the existing text inside an OutputWindowPane.
            //
            // We need another implementation (TextView / IVsTextLines /
            // Output interception / logger) to retrieve the current contents.
            //

            throw new NotSupportedException(
                "Visual Studio OutputWindowPane does not expose existing text.");
        }
    }
}