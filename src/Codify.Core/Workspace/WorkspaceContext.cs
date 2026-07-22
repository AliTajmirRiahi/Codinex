using System;

namespace Codify.Core.Workspace
{
    /// <summary>
    /// Represents the complete workspace context available to the AI.
    /// </summary>
    public sealed class WorkspaceContext
    {
        /// <summary>
        /// Gets or sets the latest snapshot of the workspace.
        /// </summary>
        public WorkspaceSnapshot Snapshot { get; set; } = new WorkspaceSnapshot();

        /// <summary>
        /// Gets or sets the accumulated workspace memory.
        /// </summary>
        public WorkspaceMemory Memory { get; set; } = new WorkspaceMemory();

        /// <summary>
        /// Gets or sets the last update time.
        /// </summary>
        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
    }
}