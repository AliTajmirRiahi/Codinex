using Codify.Core.Conversation;
using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Models;
using Codify.Core.Tools;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models.Tools;

namespace Codify.VisualStudio.Tools.BuiltIn.Search;

/// <summary>
/// FindSymbolTool
/// </summary>
[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class FindSymbolTool : IAiTool
{
    public string Name => "find_symbol";

    public string Description => "";

    public string StatusMessage => "Finding symbol...";

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
