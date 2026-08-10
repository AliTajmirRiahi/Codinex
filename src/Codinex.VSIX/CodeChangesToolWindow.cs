using System.Runtime.InteropServices;
using Codinex.UI.ToolWindows;
using Codinex.Core.Interfaces;
using Codinex.VSIX.Bootstrap;
using Microsoft.VisualStudio.Shell;

namespace Codinex.VSIX
{
    /// <summary>
    /// Tool window that hosts the AI code-changes review UI (diff, file tree, accept/reject).
    /// </summary>
    [Guid(Codinex.VisualStudio.ToolWindowGuids.CodeChangesToolWindow)]
    public class CodeChangesToolWindow : ToolWindowPane
    {
        public CodeChangesToolWindow() : base(null)
        {
            this.Caption = "Codinex AI - Code Changes";

            var tool = new CodeChangesToolWindowControl();

            tool.Initialize(CodinexServiceContainer.Instance, CodinexServiceContainer.Get<IErrorHandler>());

            this.Content = tool;
        }
    }
}
