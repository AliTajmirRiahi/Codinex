using Codify.Core.Conversation;
using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Models;
using Codify.Core.Tools;
using Codify.VisualStudio.Interfaces;
using Codify.VisualStudio.Models.Tools.ListDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Interfaces;
using Codify.Core.Models.Tools;

namespace Codify.VisualStudio.Tools.BuiltIn.Workspace
{
    [AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
    public class ListDirectoryTool(IWorkspaceFileService workspaceFileService) : IAiTool
    {
        public string Name => "list_directory";

        public string Description =>
            "Lists the files and directories within a workspace directory.";

        public ToolDefinition Definition { get; } =
            new ToolDefinition(
                new Dictionary<string, ToolProperty>
                {
                    ["path"] = new ToolProperty(
                        ToolPropertyType.String,
                        "The workspace-relative directory path to list. Use an empty string to list the workspace root.")
                },
                [
                    "path"
                ]);

        public ToolVisibility Visibility { get; } = ToolVisibility.Model;

        public async Task<ToolResult> ExecuteAsync(
            ToolRequest request,
            CancellationToken cancellationToken)
        {
            await Task.Yield();

            var path = request.GetRequiredString("path");

            var entries = await workspaceFileService.ListDirectoryAsync(path, cancellationToken);

            return ToolResult.Successful(
                request.Id,
                new
                {
                    Path = path,
                    Entries = entries.ToList()
                        .Select(e => new
                        {
                            e.Name,
                            e.RelativePath,
                            Type = e.Type ==  WorkspaceEntryType.Directory ? "Directory" : "File"
                        })
                });
        }
    }
}