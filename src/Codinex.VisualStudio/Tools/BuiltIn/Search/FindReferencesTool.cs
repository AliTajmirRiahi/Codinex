using Codinex.Core.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Models.Tools;
using Codinex.Core.Tools;

namespace Codinex.VisualStudio.Tools.BuiltIn.Search;

/// <summary>
/// FindReferencesTool
/// </summary>
[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class FindReferencesTool : IAiTool
{
    public string Name => "find_references";

    public string Description => "";

    public IReadOnlyList<string> Capabilities =>
    [
        "find references",
        "show references",
        "symbol references",
        "where used",
        "find usages"
    ];

    public string StatusMessage => "Finding references...";

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
