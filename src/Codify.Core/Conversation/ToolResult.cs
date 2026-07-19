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

    /// <summary>
    /// Creates a successful tool result.
    /// </summary>
    public static ToolResult Successful(
        string id,
        JObject data = null)
    {
        return new ToolResult
        {
            Id = id,
            Success = true,
            Data = data ?? new JObject()
        };
    }

    /// <summary>
    /// Creates a successful tool result.
    /// </summary>
    public static ToolResult Successful(
        string id,
        object data)
    {
        return new ToolResult
        {
            Id = id,
            Success = true,
            Data = JObject.FromObject(data)
        };
    }

    /// <summary>
    /// Creates a failed tool result.
    /// </summary>
    public static ToolResult Failed(
        string id,
        string error)
    {
        return new ToolResult
        {
            Id = id,
            Success = false,
            Error = error,
            Data = new JObject()
        };
    }
}