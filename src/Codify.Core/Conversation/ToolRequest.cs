using Newtonsoft.Json.Linq;

namespace Codify.Core.Conversation;

/// <summary>
/// Represents a tool execution request emitted by an AI provider.
/// </summary>
public sealed class ToolRequest
{
    /// <summary>
    /// Unique tool call identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Tool name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Provider supplied arguments.
    /// </summary>
    public JObject Arguments { get; set; } = new JObject();
}