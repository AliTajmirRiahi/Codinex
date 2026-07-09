using System;

namespace Codify.Infrastructure.VisualStudio.Models
{
    /// <summary>
    /// Represents a Visual Studio Output window pane.
    /// </summary>
    public sealed class OutputPaneInfo
    {
        /// <summary>
        /// Display name shown in the Output window.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Unique identifier of the pane when available.
        /// </summary>
        public Guid? Guid { get; set; }

        /// <summary>
        /// Indicates whether the pane is currently active.
        /// </summary>
        public bool IsActive { get; set; }
    }
}