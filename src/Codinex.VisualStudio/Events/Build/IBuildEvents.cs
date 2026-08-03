using System;

namespace Codify.VisualStudio.Events.Build
{
    /// <summary>
    /// Exposes build lifecycle events.
    /// </summary>
    public interface IBuildEvents
    {
        /// <summary>
        /// Occurs when a build starts.
        /// </summary>
        event EventHandler BuildStarted;

        /// <summary>
        /// Occurs when a build finishes.
        /// </summary>
        event EventHandler BuildCompleted;
    }
}