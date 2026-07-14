using Newtonsoft.Json.Linq;

namespace Codify.Core.Conversation;

/// <summary>
/// Represents the execution result of a tool.
/// </summary>
public sealed class ToolResult
{
    /// <summary>
    /// Tool call identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Indicates whether execution succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Structured result payload.
    /// </summary>
    public JObject Data { get; set; } = new JObject();

    /// <summary>
    /// Error message when execution fails.
    /// </summary>
    public string Error { get; set; }
}