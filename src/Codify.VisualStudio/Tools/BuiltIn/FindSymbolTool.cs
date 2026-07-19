using Codify.Core.Tools;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Conversation;
using Codify.Core.Models;

namespace Codify.VisualStudio.Tools.BuiltIn;

/// <summary>
/// FindSymbolTool
/// </summary>
public sealed class FindSymbolTool : IAiTool
{
    public string Name => "find_symbol";

    public string Description => "";

    public ToolDefinition Definition => throw new System.NotImplementedException();

    public Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
