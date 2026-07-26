using System.Collections.Generic;

namespace Codify.Core.Models
{
    public sealed class MemoryDocument
    {
        public List<MemoryFact> Facts { get; set; } = new();
    }
}