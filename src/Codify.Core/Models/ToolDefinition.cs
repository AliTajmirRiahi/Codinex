using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codify.Core.Models
{
    public sealed class ToolDefinition
    {
        public IReadOnlyDictionary<string, ToolProperty> Properties { get; }

        public IReadOnlyList<string> Required { get; }

        public ToolDefinition(
            IReadOnlyDictionary<string, ToolProperty> properties,
            IReadOnlyList<string> required)
        {
            Properties = properties;
            Required = required;
        }
    }
}
