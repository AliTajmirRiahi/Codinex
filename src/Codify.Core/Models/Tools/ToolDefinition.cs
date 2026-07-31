using System.Collections.Generic;

namespace Codify.Core.Models.Tools
{
    public sealed class ToolDefinition(
        IReadOnlyDictionary<string, ToolProperty> properties,
        IReadOnlyList<string> required,
        bool endsConversation = false)
    {
        public IReadOnlyDictionary<string, ToolProperty> Properties { get; } = properties;

        public IReadOnlyList<string> Required { get; } = required;

        public bool EndsConversation { get; set; } = endsConversation;
    }
}
