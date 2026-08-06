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
using Codinex.VisualStudio.Models;

namespace Codinex.VisualStudio.Tools.BuiltIn.Files;

/// <summary>
/// Returns the complete implementation of a requested code element.
/// </summary>
[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class ReadElementTool(
    IWorkspaceFileService workspaceFileService,
    IWorkspaceSearchService workspaceSearchService)
    : IAiTool
{
    public string Name => "read_element";

    public string Description =>
        "Return the complete implementation of the requested code element. " +
        "Input must be an element id returned by get_file_elements.";

    public string StatusMessage => "Reading element...";

    public ToolVisibility Visibility { get; } = ToolVisibility.Model;

    public ToolDefinition Definition { get; } =
        new ToolDefinition(
            new Dictionary<string, ToolProperty>
            {
                ["elementId"] = new ToolProperty(
                    ToolPropertyType.String,
                    "The stable element id returned by get_file_elements.")
            },
            [
                "elementId"
            ]);

    public async Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Yield();

        try
        {
            var elementId = request.GetRequiredString("elementId");
            var files = GetSupportedSourceFiles();

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var content = await workspaceFileService.ReadAsync(file.FullPath, cancellationToken);
                var element = SourceFileElementParser.Parse(file.RelativePath, content)
                    .FirstOrDefault(e => e.Id.Equals(elementId, StringComparison.Ordinal));

                if (element == null)
                {
                    continue;
                }

                return ToolResult.Successful(
                    request.Id,
                    new
                    {
                        id = element.Id,
                        source = element.Source
                    });
            }

            return ToolResult.Failed(
                request.Id,
                $"No element matching id '{elementId}' was found.");
        }
        catch (Exception ex)
        {
            return ToolResult.Failed(
                request.Id,
                ex.Message);
        }
    }

    private IReadOnlyList<WorkspaceFile> GetSupportedSourceFiles()
    {
        return SourceFileElementParser.SupportedExtensions
            .SelectMany(extension => workspaceSearchService.FindByExtension(extension))
            .Where(file => SourceFileElementParser.IsSupported(file.RelativePath))
            .GroupBy(file => file.FullPath, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }
}
