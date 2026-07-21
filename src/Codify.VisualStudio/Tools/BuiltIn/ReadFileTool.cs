using Codify.Core.Conversation;
using Codify.Core.Models;
using Codify.Core.Tools;
using Codify.VisualStudio.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.VisualStudio.Tools.BuiltIn;

/// <summary>
/// ReadFileTool
/// </summary>
public sealed class ReadFileTool(IWorkspaceFileService workspaceFileService, IWorkspaceFileLocator workspaceFileLocator) : IAiTool
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
        await Task.Yield();

        var query = request.GetRequiredString("path");

        var files = workspaceFileLocator.Find(query);

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

        var content = workspaceFileService.ReadFile(file.FullPath);

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
