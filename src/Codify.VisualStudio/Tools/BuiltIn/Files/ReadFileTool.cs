using Codify.Core.Conversation;
using Codify.Core.Models;
using Codify.Core.Tools;
using Codify.VisualStudio.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.VisualStudio.Tools.BuiltIn.Files;

/// <summary>
/// ReadFileTool
/// </summary>
public sealed class ReadFileTool(IWorkspaceFileService workspaceFileService, IWorkspaceSearchService workspaceSearchService) : IAiTool
{
    public string Name => "read_file";

    public string Description => "Reads the contents of a file from the workspace.";
    public ToolVisibility Visibility { get; } = ToolVisibility.Model;


    public ToolDefinition Definition { get; } =
        new ToolDefinition(
            new Dictionary<string, ToolProperty>
            {
                ["path"] = new ToolProperty(
                     ToolPropertyType.String,
                    "Reads a file from the current workspace.\r\n\r\nUse this tool ONLY when the file path is known.\r\nDo NOT call this tool if you don't know the file path.")
            },
            [
                "path"
            ]);

    public async Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Yield();

        var query = request.GetRequiredString("path");

        var files = workspaceSearchService.FindFiles(query);

        switch (files.Count)
        {
            case 0:
                return ToolResult.Failed(
                    request.Id,
                    $"No file matching '{query}' was found.");
            case > 1:
                return ToolResult.Successful(
                    request.Id,
                    new
                    {
                        matches = files.Select(f => new
                        {
                            f.Name,
                            f.RelativePath
                        })
                    });
        }

        var file = files[0];

        var content = await workspaceFileService.ReadAsync(file.FullPath, cancellationToken);

        return ToolResult.Successful(
            request.Id,
            new
            {
                file.Name,
                file.RelativePath,
                Content = content
            });
    }
}
