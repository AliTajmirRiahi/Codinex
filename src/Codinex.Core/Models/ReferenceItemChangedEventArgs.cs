using System;

namespace Codinex.Core.Models
{
    public sealed class ReferenceItemChangedEventArgs(ReferenceItem item) : EventArgs
    {
        public ReferenceItem Item { get; } = item;
    }
}
