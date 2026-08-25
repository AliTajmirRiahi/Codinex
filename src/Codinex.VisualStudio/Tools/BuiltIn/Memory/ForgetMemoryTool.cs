using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Models.Tools;
using Codinex.Core.Tools;
using Codinex.Storage.Interfaces;

namespace Codinex.VisualStudio.Tools.BuiltIn.Memory;

/// <summary>
/// Removes a long-term workspace memory.
/// </summary>
[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class ForgetMemoryTool(IMemoryManager memoryManager) : IAiTool
{
    public string Name => "forget_memory";

    public string Description =>
        "Removes a previously stored workspace memory.";

    public IReadOnlyList<string> Capabilities =>
    [
        "forget memory",
        "remove memory",
        "delete memory",
        "clear memory",
        "forget stored memory"
    ];

    public string StatusMessage => "Forgetting memory...";

    public ToolVisibility Visibility { get; } = ToolVisibility.Model;

    public ToolDefinition Definition { get; } =
        new ToolDefinition(
            new Dictionary<string, ToolProperty>
            {
                ["id"] = new ToolProperty(
                    ToolPropertyType.String,
                    "The unique identifier of the memory to remove.")
            },
            [
                "id"
            ]);

    public async Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        var id = request.GetRequiredString("id");

        if (!System.Guid.TryParse(id, out var memoryId))
        {
            return ToolResult.Failed(
                request.Id,
                $"'{id}' is not a valid memory id.");
        }

        var memory = memoryManager.Get(memoryId);

        await memoryManager.ForgetAsync(memoryId);

        return ToolResult.Successful(
            request.Id,
            new
            {
                memory.Id,
                memory.Title,
                Deleted = true
            });
    }
}