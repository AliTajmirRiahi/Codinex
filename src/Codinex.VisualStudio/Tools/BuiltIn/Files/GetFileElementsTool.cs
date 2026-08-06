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
using Codinex.VisualStudio.Interfaces;

namespace Codinex.VisualStudio.Tools.BuiltIn.Files;

/// <summary>
/// Returns the structural outline of a source file without returning implementations.
/// </summary>
[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class GetFileElementsTool(
    IWorkspaceFileService workspaceFileService,
    IWorkspaceSearchService workspaceSearchService)
    : IAiTool
{
    public string Name => "get_file_elements";

    public string Description =>
        "Return the structural outline of a source file without returning its implementation. " +
        "The returned element kinds depend on the programming language.";

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

            if (!SourceFileElementParser.IsSupported(file.RelativePath))
            {
                return ToolResult.Failed(
                    request.Id,
                    $"Unsupported source file type '{file.Name}'.");
            }

            var content = await workspaceFileService.ReadAsync(file.FullPath, cancellationToken);
            var elements = SourceFileElementParser.Parse(file.RelativePath, content);

            return ToolResult.Successful(
                request.Id,
                new
                {
                    file = SourceFileElementParser.NormalizePath(file.RelativePath),
                    language = SourceFileElementParser.GetLanguage(file.RelativePath),
                    elements = elements
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
