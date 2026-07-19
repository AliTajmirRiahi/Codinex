using Codify.Core.Tools;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Conversation;
using Codify.Core.Models;

namespace Codify.VisualStudio.Tools.BuiltIn;

/// <summary>
/// GetDiagnosticsTool
/// </summary>
public sealed class GetDiagnosticsTool : IAiTool
{
    public string Name => "get_diagnostics";

    public string Description => "";

    public ToolDefinition Definition => throw new System.NotImplementedException();

    public Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
