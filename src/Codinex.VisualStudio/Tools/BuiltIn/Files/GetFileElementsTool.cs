using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Models;
using Codinex.Core.Models.Tools;
using Codinex.Core.Tools;
using Codinex.VisualStudio.Interfaces;

namespace Codinex.VisualStudio.Tools.BuiltIn.Files;

/// <summary>
/// Returns the structural outline of a source file without returning implementations.
/// </summary>
[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class GetFileElementsTool(
    IWorkspaceSearchService workspaceSearchService,
    ISourceFileElementService sourceFileElementService)
    : IAiTool
{
    public string Name => "get_file_elements";

    public string Description =>
        "Return the structural outline of a source file without returning its implementation. " +
        "The returned element kinds depend on the programming language.";

    public IReadOnlyList<string> Capabilities =>
    [
        "get file elements",
        "show file outline",
        "inspect file structure",
        "list classes",
        "list methods",
        "summarize file structure",
        "source outline"
    ];

    public string StatusMessage => "Getting file elements...";

    public ToolVisibility Visibility { get; } = ToolVisibility.Model;

    public ToolDefinition Definition { get; } =
        new ToolDefinition(
            new Dictionary<string, ToolProperty>
            {
                ["filePath"] = new ToolProperty(
                    ToolPropertyType.String,
                    "The workspace-relative path of the source file to inspect.")
            },
            [
                "filePath"
            ]);

    public async Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Yield();

        try
        {
            var query = request.GetRequiredString("filePath");
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

            if (!sourceFileElementService.IsSupported(file.RelativePath))
            {
                return ToolResult.Failed(
                    request.Id,
                    $"Unsupported source file type '{file.Name}'.");
            }

            var outline = sourceFileElementService.GetFileOutline(file.FullPath, file.RelativePath);

            return ToolResult.Successful(
                request.Id,
                new
                {
                    file = outline.File,
                    language = outline.Language,
                    elements = outline.Elements
                        .OrderBy(e => e.Order)
                        .Select(e => new
                        {
                            id = e.Id,
                            kind = e.Kind,
                            name = e.Name,
                            signature = e.Signature
                        })
                });
        }
        catch (Exception ex)
        {
            return ToolResult.Failed(
                request.Id,
                ex.Message);
        }
    }
}
