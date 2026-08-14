using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Core.Conversation;

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
    public object Data { get; set; }

    /// <summary>
    /// Error message when execution fails.
    /// </summary>
    public string Error { get; set; }

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
            Data = data
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
        };
    }
    /// <summary>
    /// Creates a failed tool result.
    /// </summary>
    public static ToolResult Failed(
        string id,
        string error,
        object data)
    {
        return new ToolResult
        {
            Id = id,
            Success = false,
            Error = error,
            Data = data
        };
    }
    /// <summary>
    /// Creates a failed tool result.
    /// </summary>
    public static ToolResult Failed(
        string id,
        IReadOnlyList<WorkspaceValidationError> errors)
    {
        return new ToolResult
        {
            Id = id,
            Success = false,
            Error = "Workspace validation failed.",
            Data = new
            {
                errors = errors.Select(x => new
                {
                    changeId = x.ChangeId,
                    code = x.Code.ToString(),
                    category = x.Category.ToString(),
                    message = x.Message
                })
            }
        };
    }

    /// <summary>
    /// Creates a failed tool result.
    /// </summary>
    public static ToolResult Failed(
        string id,
        IReadOnlyList<ChangeValidationError> errors)
    {
        return new ToolResult
        {
            Id = id,
            Success = false,
            Error = "Workspace validation failed.",
            Data = new
            {
                errors = errors.Select(x => new
                {
                    changeId = x.ChangeId,
                    code = x.Code.ToString(),
                    category = x.Category.ToString(),
                    message = x.Message
                })
            }
        };
    }
}