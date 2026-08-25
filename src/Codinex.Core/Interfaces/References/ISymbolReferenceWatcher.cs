using System;
using Codinex.Core.Models.References;

namespace Codinex.Core.Interfaces.References
{
    /// <summary>
    /// Watches the live Roslyn workspace for symbol-level changes (methods, classes, fields,
    /// interfaces) so consumers can keep symbol-based reference lists in sync without
    /// re-scanning the whole solution. Unlike a file-system watcher, this reacts to live
    /// buffer edits and reports the specific symbols that were added, removed, or changed.
    /// </summary>
    public interface ISymbolReferenceWatcher
    {
        event EventHandler<ReferenceItemChangedEventArgs> ReferenceAdded;
        event EventHandler<ReferenceRemovedEventArgs> ReferenceRemoved;
        event EventHandler<ReferenceItemChangedEventArgs> ReferenceUpdated;
    }
}
