using System.Collections.Generic;
using System.Threading.Tasks;
using Codify.VisualStudio.Models;

namespace Codify.VisualStudio.Interfaces
{
    /// <summary>
    /// Provides access to Visual Studio Output window panes.
    /// </summary>
    public interface IVsOutputWindowService
    {
        /// <summary>
        /// Returns all available output panes.
        /// </summary>
        Task<IReadOnlyList<OutputPaneInfo>> GetOutputPanesAsync();

        /// <summary>
        /// Asynchronously reads the output from the specified output window pane.
        /// </summary>
        /// <remarks>Ensure that the specified pane exists and is currently accessible. An exception may
        /// be thrown if the pane is not found or if an error occurs while reading the output.</remarks>
        /// <param name="paneName">The name of the output window pane to read. This must be a valid and accessible pane identifier.</param>
        /// <returns>A task that represents the asynchronous read operation. The task result contains the output from the
        /// specified pane as a string.</returns>
        Task<string> ReadOutputAsync(string paneName);
    }
}