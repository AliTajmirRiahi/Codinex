using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Conversation;
using Codify.Core.Models;
using Codify.Core.Tools;

namespace Codify.VisualStudio.Tools.BuiltIn.Search;

/// <summary>
/// FindReferencesTool
/// </summary>
public sealed class FindReferencesTool : IAiTool
{
    public string Name => "find_references";

    public string Description => "";

    public ToolVisibility Visibility { get; } = ToolVisibility.Model;
    public ToolDefinition Definition => new ToolDefinition(
        new Dictionary<string, ToolProperty>
        {

        },
        [
        ]);

    public Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
