namespace Codify.Core.Conversation;

/// <summary>
/// Represents the result of a tool execution.
/// </summary>
public sealed class ToolResult
{
    /// <summary>
    /// Tool name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Indicates whether execution succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Result payload.
    /// </summary>
    public object Data { get; set; }

    /// <summary>
    /// Optional error message.
    /// </summary>
    public string ErrorMessage { get; set; }
}