using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Conversation;

namespace Codify.Core.Tools;

/// <summary>
/// Represents an executable AI tool.
/// </summary>
public interface IAiTool
{
    /// <summary>
    /// Gets the unique tool name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the tool.
    /// </summary>
    Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken = default);
}