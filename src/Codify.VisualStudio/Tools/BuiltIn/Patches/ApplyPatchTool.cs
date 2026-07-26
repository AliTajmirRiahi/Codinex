using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Conversation;
using Codify.Core.Models;
using Codify.Core.Tools;

namespace Codify.VisualStudio.Tools.BuiltIn.Patches;
/// <summary>
/// ApplyPatchTool
/// </summary>
public sealed class ApplyPatchTool : IAiTool
{
    public string Name => "apply_patch";

    public string Description => "";

    public ToolDefinition Definition => new ToolDefinition(
        new Dictionary<string, ToolProperty>
        {
           
        },
        [
        ]);

    public ToolVisibility Visibility { get; } = ToolVisibility.Model;

    public Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
