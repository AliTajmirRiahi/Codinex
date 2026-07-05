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
        Folder = 7,
        Method = 8,
        Class = 9,
    }

    public sealed class ReferenceItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ReferenceKind Type { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public ReferenceMetadata Metadata { get; set; }
    }

    public sealed class ReferenceMetadata
    {
        public string FilePath { get; set; }
        public string ProjectName { get; set; }
        public string ContainerName { get; set; }
        public string Signature { get; set; }
        public string Body { get; set; }
        public string Content { get; set; }
        public int? StartLine { get; set; }
        public int? EndLine { get; set; }
    }
}
