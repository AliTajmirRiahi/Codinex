using Codify.Core.Conversation;
using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Models;
using Codify.Core.Tools;
using Codify.Core.Workspace;
using Codify.VisualStudio.Interfaces;
using Codify.VisualStudio.Models;
using Codify.VisualStudio.Models.Tools.SearchProject;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models.Tools;

namespace Codify.VisualStudio.Tools.BuiltIn.Search;

/// <summary>
/// Searches files and source code within the current workspace.
/// </summary>
[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class SearchProjectTool(IWorkspaceSearchService workspaceSearchService) : IAiTool
{
    private const int DefaultMaxResults = 20;

    public string Name => "search_project";

    public string Description =>
        "Search files and source code within the current workspace.\n\n" +
        "Supports:\n" +
        "- file name search\n" +
        "- extension search\n" +
        "- wildcard pattern search\n" +
        "- text search\n" +
        "- regular expression search";

    public ToolVisibility Visibility => ToolVisibility.Model;

    public ToolDefinition Definition => new(
        new Dictionary<string, ToolProperty>
        {
            ["query"] = new(
                ToolPropertyType.String,
                "The text, symbol, filename, or pattern to search for."),

            ["type"] = new(
                ToolPropertyType.String,
                "Search type. Valid values: fileName, extension, pattern, text, regex."),

            ["maxResults"] = new(
                ToolPropertyType.Integer,
                "Maximum number of results to return. Default is 20.")
        },
        ["query", "type"]);

    public Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var query = request.GetRequiredString("query");

            var type = request.GetRequiredString("type");

            if (!Enum.TryParse<SearchProjectType>(
                    type,
                    true,
                    out var searchType))
            {
                return Task.FromResult(
                    ToolResult.Failed(
                        request.Id,
                        $"Unsupported search type '{type}'."));
            }

            var maxResults = request.GetInt32("maxResults");

            if (maxResults <= 0)
            {
                maxResults = DefaultMaxResults;
            }

            var results = workspaceSearchService.Search(query, searchType);

            var totalCount = results.Count;

            var limitedResults = results
                .Take(maxResults)
                .ToList();

            var data = new JObject
            {
                ["query"] = query,
                ["type"] = type,
                ["totalCount"] = totalCount,
                ["returnedCount"] = limitedResults.Count,
                ["isTruncated"] = totalCount > limitedResults.Count,
                ["results"] = JArray.FromObject(limitedResults)
            };

            return Task.FromResult(
                ToolResult.Successful(
                    request.Id,
                    data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                ToolResult.Failed(
                    request.Id,
                    ex.Message));
        }
    }
}