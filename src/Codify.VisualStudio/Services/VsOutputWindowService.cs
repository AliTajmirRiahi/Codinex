using Codify.Core.Interfaces;
using Codify.VisualStudio.Interfaces;
using Codify.VisualStudio.Internal;
using Codify.VisualStudio.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


#pragma warning disable VSTHRD010
namespace Codify.VisualStudio.Services
{
    /// <summary>
    /// Reads information from the Visual Studio Output window.
    /// </summary>
    public sealed class VsOutputWindowService(IVisualStudioServices visualStudio, IUiThreadDispatcher uiThreadDispatcher)
        : VsServiceBase(visualStudio), IVsOutputWindowService
    {
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

        public async Task<string> ReadOutputAsync(string paneName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await uiThreadDispatcher.SwitchToMainThreadAsync();

            var dte = await GetDteAsync();

            var outputWindow =
                (OutputWindow)dte.Windows
                    .Item(EnvDTE.Constants.vsWindowKindOutput)
                    .Object;

            var pane =
                outputWindow.OutputWindowPanes
                    .Cast<OutputWindowPane>()
                    .FirstOrDefault(x =>
                        string.Equals(
                            x.Name,
                            paneName,
                            StringComparison.OrdinalIgnoreCase));

            if (pane == null)
            {
                return string.Empty;
            }

            try
            {
                dynamic dynamicPane = pane;

                var textDocument = dynamicPane.TextDocument;

                var start =
                    textDocument.StartPoint.CreateEditPoint();

                return start.GetText(textDocument.EndPoint);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
#pragma warning restore VSTHRD010