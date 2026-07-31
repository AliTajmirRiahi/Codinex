using Codify.Core.Conversation;
using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Models;
using Codify.Core.Tools;
using Codify.Storage.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models.Tools;

namespace Codify.VisualStudio.Tools.BuiltIn.Memory;

/// <summary>
/// Removes a long-term workspace memory.
/// </summary>
[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class ForgetMemoryTool(IMemoryManager memoryManager) : IAiTool
{
    public string Name => "forget_memory";

    public string Description =>
        "Removes a previously stored workspace memory.";

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