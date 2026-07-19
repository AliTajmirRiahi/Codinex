using Codify.Core.Conversation;
using Codify.Core.Models;
using Codify.Core.Tools;
using Codify.VisualStudio.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.VisualStudio.Tools.BuiltIn;

/// <summary>
/// ReadFileTool
/// </summary>
public sealed class ReadFileTool(IWorkspaceFileService workspaceFileService) : IAiTool
{
    public string Name => "read_file";

    public string Description => "Reads the contents of a file from the workspace.";

    public ToolDefinition Definition { get; } =
        new ToolDefinition(
            new Dictionary<string, ToolProperty>
            {
                ["path"] = new ToolProperty(
                     ToolPropertyType.String,
                    "The relative path of the file to read.")
            },
            [
                "path"
            ]);

    public async Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(1);

        var path = request.GetRequiredString("path");

        var content = workspaceFileService.ReadFile(path);

        return ToolResult.Successful(content);
    }
}
