using System;

namespace Codinex.Core.Models
{
    public sealed class ReferenceRemovedEventArgs(string id) : EventArgs
    {
        public string Id { get; } = id;
    }
}
