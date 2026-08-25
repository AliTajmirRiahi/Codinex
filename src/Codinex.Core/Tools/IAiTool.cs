using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.Models.Tools;

namespace Codinex.Core.Tools;


public enum ToolVisibility
{
    Model = 0,

    Internal = 1,

    Debug = 2,

    Experimental = 3,
}
/// <summary>
/// Represents an executable AI tool.
/// </summary>
public interface IAiTool
{
    /// <summary>
    /// Gets the unique tool name.
    /// </summary>
    string Name { get; }

    string Description { get; }

    IReadOnlyList<string> Capabilities { get; }

    string StatusMessage { get; }

    ToolDefinition Definition { get; }

    ToolVisibility Visibility { get; }

    /// <summary>
    /// Executes the tool.
    /// </summary>
    Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken = default);
}