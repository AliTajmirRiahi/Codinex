using System.Runtime.InteropServices;
using Codify.Core.Interfaces;
using Codify.UI.ToolWindows;
using Codify.VSIX.Bootstrap;
using Microsoft.VisualStudio.Shell;

namespace Codify.VSIX
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("0b1df633-2a89-484e-98a4-0347b31850f8")]
    public class CodifyToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodifyToolWindow"/> class.
        /// </summary>
        public CodifyToolWindow() : base(null)
        {
            this.Caption = "Codify AI";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            var tool = new CodifyToolWindowControl();

            tool.Initialize(CodifyServiceContainer.Instance, CodifyServiceContainer.Get<IErrorHandler>());

            this.Content = tool;
        }
    }
}
