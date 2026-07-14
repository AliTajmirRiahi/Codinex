using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Codify.Core.Conversation;

/// <summary>
/// Represents a tool request generated during a conversation.
/// </summary>
public sealed class ToolRequest
{
    /// <summary>
    /// Tool unique name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Tool arguments.
    /// </summary>
    public JObject Arguments { get; set; }
}