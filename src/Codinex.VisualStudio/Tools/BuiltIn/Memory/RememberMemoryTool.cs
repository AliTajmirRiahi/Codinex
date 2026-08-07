using Codinex.Core.Models;
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
/// Stores a long-term workspace memory.
/// </summary>
[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class RememberMemoryTool(IMemoryManager memoryManager) : IAiTool
{
    public string Name => "remember_memory";

    public string Description =>
        "Stores or updates a long-term workspace memory.";

    public IReadOnlyList<string> Capabilities =>
    [
        "remember memory",
        "store memory",
        "save memory",
        "remember this",
        "persist workspace memory",
        "add memory"
    ];

    public string StatusMessage => "Remembering memory...";

    public ToolVisibility Visibility { get; } = ToolVisibility.Model;

    public ToolDefinition Definition { get; } =
        new ToolDefinition(
            new Dictionary<string, ToolProperty>
            {
                ["title"] = new ToolProperty(
                    ToolPropertyType.String,
                    "Short title describing the memory."),

                ["content"] = new ToolProperty(
                    ToolPropertyType.String,
                    "Detailed memory content.")
            },
            [
                "title",
                "content"
            ]);

    public async Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        var title = request.GetRequiredString("title");
        var content = request.GetRequiredString("content");

        var memory = await memoryManager.RememberAsync(
            title,
            content);

        return ToolResult.Successful(
            request.Id,
            new
            {
                memory.Id,
                memory.Title,
                memory.Content,
                memory.CreatedAt,
                memory.UpdatedAt
            });
    }
}