using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Conversation;
using Codify.Core.Models;
using Codify.Core.Tools;

namespace Codify.VisualStudio.Tools.BuiltIn.Diagnostics;

/// <summary>
/// GetDiagnosticsTool
/// </summary>
public sealed class GetDiagnosticsTool : IAiTool
{
    public string Name => "get_diagnostics";

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
