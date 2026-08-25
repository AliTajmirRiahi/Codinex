using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Models.Tools;
using Codinex.Core.Tools;

namespace Codinex.VisualStudio.Tools.BuiltIn.Tests;

/// <summary>
/// RunTestsTool
/// </summary>
[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class RunTestsTool : IAiTool
{
    public string Name => "run_tests";

    public string Description => "";

    public IReadOnlyList<string> Capabilities =>
    [
        "run tests",
        "execute tests",
        "test project",
        "run unit tests",
        "run all tests"
    ];

    public string StatusMessage => "Running tests...";

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
