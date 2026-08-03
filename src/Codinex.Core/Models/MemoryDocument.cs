using System.Collections.Generic;

namespace Codinex.Core.Models
{
    public sealed class MemoryDocument
    {
        public List<MemoryFact> Facts { get; set; } = new();
    }
}