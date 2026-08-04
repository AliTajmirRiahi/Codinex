using System.Collections.Generic;

namespace Codinex.Core.Models
{
    public sealed class PromptSectionProfile
    {
        public string Name { get; set; }

        public int Characters { get; set; }

        public int EstimatedTokens { get; set; }

        public double SectionPercentage { get; set; }

        public string Reason { get; set; }

        public IReadOnlyList<PromptSectionProfile> Children { get; set; } = new List<PromptSectionProfile>();
    }
}
