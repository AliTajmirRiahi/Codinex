using System.Collections.Generic;

namespace Codinex.Core.Models.Context
{
    public sealed class MemoryDocument
    {
        public List<MemoryFact> Facts { get; set; } = new();
    }
}