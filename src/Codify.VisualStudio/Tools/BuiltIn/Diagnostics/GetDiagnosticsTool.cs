using Codify.Core.Conversation;
using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Models;
using Codify.Core.Tools;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models.Tools;

namespace Codify.VisualStudio.Tools.BuiltIn.Diagnostics;

/// <summary>
/// GetDiagnosticsTool
/// </summary>
[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class GetDiagnosticsTool : IAiTool
{
    public string Name => "get_diagnostics";

    public string Description => "";

    public string StatusMessage => "Getting diagnostics...";

    public ToolVisibility Visibility { get; } = ToolVisibility.Debug;

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
