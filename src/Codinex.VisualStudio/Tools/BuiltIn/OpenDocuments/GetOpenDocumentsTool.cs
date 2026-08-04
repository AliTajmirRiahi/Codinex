using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models.Tools;
using Codinex.Core.Tools;

namespace Codinex.VisualStudio.Tools.BuiltIn.OpenDocuments;

/// <summary>
/// Gets the currently open documents in Visual Studio.
/// </summary>
[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class GetOpenDocumentsTool(
    IOpenDocumentsProvider openDocumentsProvider)
    : IAiTool
{
    public string Name => "get_open_documents";

    public string Description =>
        "Gets the currently open documents in Visual Studio.";

    public string StatusMessage => "Getting open documents...";

    public ToolVisibility Visibility { get; } = ToolVisibility.Model;

    public ToolDefinition Definition => new(
        new Dictionary<string, ToolProperty>(),
        []);

    public async Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var documents = await openDocumentsProvider
                .GetOpenDocumentsAsync(cancellationToken);

            documents ??= [];

            return ToolResult.Successful(
                request.Id,
                new
                {
                    Count = documents.Count,
                    Documents = documents
                        .OrderBy(document => document.Name)
                        .ThenBy(document => document.Value)
                        .Select(document => new
                        {
                            document.Id,
                            document.Name,
                            document.Description,
                            Type = document.Type.ToString(),
                            document.Value,
                            document.Metadata.FilePath,
                            document.Metadata.ProjectName,
                            document.Metadata.ContainerName,
                            document.Metadata.StartLine,
                            document.Metadata.EndLine
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
