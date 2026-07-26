using Codify.Core.Conversation;
using Codify.Core.Models;
using Codify.Core.Tools;
using Codify.Storage.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.VisualStudio.Tools.BuiltIn;

/// <summary>
/// Stores a long-term workspace memory.
/// </summary>
public sealed class RememberMemoryTool(IMemoryManager memoryManager) : IAiTool
{
    public string Name => "remember_memory";

    public string Description =>
        "Stores or updates a long-term workspace memory.";

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