using Codify.Core.Conversation;
using Codify.Core.Models;
using Codify.Core.Tools;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.VisualStudio.Tools.BuiltIn;

/// <summary>
/// GetDiagnosticsTool
/// </summary>
public sealed class GetDiagnosticsTool : IAiTool
{
    public string Name => "get_diagnostics";

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
