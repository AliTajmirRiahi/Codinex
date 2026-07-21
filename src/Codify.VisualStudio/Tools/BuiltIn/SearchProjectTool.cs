using Codify.Core.Conversation;
using Codify.Core.Models;
using Codify.Core.Tools;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.VisualStudio.Tools.BuiltIn;

/// <summary>
/// SearchProjectTool
/// </summary>
public sealed class SearchProjectTool : IAiTool
{
    public string Name => "search_project";

    public string Description => "";

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
