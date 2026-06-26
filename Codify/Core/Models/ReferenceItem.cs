using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codify.Core.Models
{
    public enum ReferenceKind
    {
        File = 1,
        Solution = 2,
        Output = 3,
        Log = 4,
        Git = 5,
        Project = 6,
        Folder = 7
    }

    public class ReferenceItem
    {
        public string Id { get; set; }
        // The main display name (e.g., "ExecutionPipeline.cs")
        public string Name { get; set; }

        // The sub-text or secondary info (e.g., "Execution\ExecutionPipeline.cs")
        public string Description { get; set; }

        // Categorization to help UI logic (e.g., "file", "log", "system")
        public ReferenceKind Type { get; set; }

        // Identifier for the icon rendering (e.g., "icon-file-code")
        public string Icon { get; set; }

        // The raw payload if specific action is needed (e.g., file path or ID)
        public string Value { get; set; }
    }
}
