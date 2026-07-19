using Codify.Core.Tools;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Conversation;
using Codify.Core.Models;

namespace Codify.VisualStudio.Tools.BuiltIn;

/// <summary>
/// FindReferencesTool
/// </summary>
public sealed class FindReferencesTool : IAiTool
{
    public string Name => "find_references";

    public string Description => "";

    public ToolDefinition Definition => throw new System.NotImplementedException();

    public Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
