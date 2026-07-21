using Codify.Core.Conversation;
using Codify.Core.Models;
using Codify.Core.Tools;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.VisualStudio.Tools.BuiltIn;

/// <summary>
/// FindReferencesTool
/// </summary>
public sealed class FindReferencesTool : IAiTool
{
    public string Name => "find_references";

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
