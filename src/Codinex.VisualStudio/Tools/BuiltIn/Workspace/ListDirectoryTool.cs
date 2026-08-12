using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.Models.Tools.ListDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Core.Models.Tools;
using Codinex.Core.Tools;

namespace Codinex.VisualStudio.Tools.BuiltIn.Workspace
{
    [AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
    public class ListDirectoryTool(IWorkspaceFileService workspaceFileService, IWorkspaceContext workspaceContext) : IAiTool
    {
        public string Name => "list_directory";

        public string Description =>
            "Lists the files and directories within a workspace directory. It Should Have path";

        public IReadOnlyList<string> Capabilities =>
        [
            "list directory",
            "show directory",
            "browse folder",
            "list files",
            "show files",
            "workspace tree",
            "directory contents"
        ];

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

        public string StatusMessage => "Listing directory...";

        public async Task<ToolResult> ExecuteAsync(
            ToolRequest request,
            CancellationToken cancellationToken)
        {
            await Task.Yield();

            var path = request.GetRequiredString("path");

            if (string.IsNullOrEmpty(path))
                path = workspaceContext.SolutionPath;

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
                            Type = e.Type == WorkspaceEntryType.Directory ? "Directory" : "File"
                        })
                });
        }
    }
}