using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Models.Tools;
using Codinex.Core.Tools;
using Codinex.VisualStudio.Interfaces;

namespace Codinex.VisualStudio.Tools.BuiltIn.Files;

/// <summary>
/// Returns the complete implementation of a requested code element.
/// </summary>
[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class ReadElementTool(
    ISourceFileElementService sourceFileElementService)
    : IAiTool
{
    public string Name => "read_element";

    public string Description =>
        "Return the complete implementation of the requested code element. " +
        "Input must be an element id returned by get_file_elements.";

    public IReadOnlyList<string> Capabilities =>
    [
        "read element",
        "read code element",
        "show method implementation",
        "show class implementation",
        "inspect element source",
        "read function"
    ];

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
            cancellationToken.ThrowIfCancellationRequested();

            var elementId = request.GetRequiredString("elementId");
            var element = await Task.Run(
                () => sourceFileElementService.FindElement(elementId),
                cancellationToken);

            if (element == null)
            {
                return ToolResult.Failed(
                    request.Id,
                    $"No element matching id '{elementId}' was found.");
            }

            return ToolResult.Successful(
                request.Id,
                new
                {
                    id = element.Id,
                    source = element.Source
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
