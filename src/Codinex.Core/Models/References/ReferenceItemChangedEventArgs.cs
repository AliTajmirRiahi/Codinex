using System;

namespace Codinex.Core.Models.References
{
    public sealed class ReferenceItemChangedEventArgs(ReferenceItem item) : EventArgs
    {
        public ReferenceItem Item { get; } = item;
    }
}
