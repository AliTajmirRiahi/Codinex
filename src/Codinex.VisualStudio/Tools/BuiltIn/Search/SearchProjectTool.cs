using Codinex.Core.Models;
using Codinex.Core.Workspace;
using Codinex.VisualStudio.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Models.Tools;
using Codinex.Core.Tools;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.Models.Tools.SearchProject;

namespace Codinex.VisualStudio.Tools.BuiltIn.Search;

/// <summary>
/// Searches files and source code within the current workspace.
/// </summary>
[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class SearchProjectTool(IWorkspaceSearchService workspaceSearchService) : IAiTool
{
    private const int DefaultCount = 5;

    public string Name => "search_project";

    public string Description =>
        "Search files and source code within the current workspace.\n\n" +
        "Supports:\n" +
        "- file name search\n" +
        "- extension search\n" +
        "- wildcard pattern search\n" +
        "- text search\n" +
        "- regular expression search";

    public IReadOnlyList<string> Capabilities =>
    [
        "search project",
        "search workspace",
        "find text",
        "find file",
        "search files",
        "regex search",
        "search source code",
        "find in files"
    ];

    public ToolVisibility Visibility => ToolVisibility.Model;

    public string StatusMessage => "Searching project...";

    public ToolDefinition Definition => new(
        new Dictionary<string, ToolProperty>
        {
            ["query"] = new(
                ToolPropertyType.String,
                "The text, symbol, filename, or pattern to search for."),

            ["type"] = new(
                ToolPropertyType.String,
                "Search type. Valid values: fileName, extension, pattern, text, regex."),

            ["skip"] = new(
                ToolPropertyType.Integer,
                "Skip is A factor of 5. Default is 5."),

            ["take"] = new(
                ToolPropertyType.Integer,
                "Take number of results to return. Max is 5.")
        },
        ["query", "type", "skip", "take"]);

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

            var skip = request.GetInt32("skip");

            var take = request.GetInt32("take");

            if (take <= 0)
                take = DefaultCount;

            var results = workspaceSearchService.Search(query, searchType);

            var totalCount = results.Count;

            var limitedResults = results
                .Skip(skip)
                .Take(take)
                .ToList();

            var tt = Newtonsoft.Json.JsonConvert.SerializeObject(limitedResults);

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