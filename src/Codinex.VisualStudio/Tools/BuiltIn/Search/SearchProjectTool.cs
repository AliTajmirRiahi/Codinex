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
    private const int DefaultCount = 20;

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
                $"Skip is A factor of {DefaultCount}. Default is {DefaultCount}."),

            ["take"] = new(
                ToolPropertyType.Integer,
                $"Take number of results to return. Max is {DefaultCount}.")
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

            // FullPath is intentionally omitted: it repeats the same workspace-root prefix on
            // every row and is redundant with RelativePath, which is all downstream tools need.
            var data = new
            {
                Query = query,
                Type = type,
                TotalCount = totalCount,
                ReturnedCount = limitedResults.Count,
                IsTruncated = totalCount > limitedResults.Count,
                Results = limitedResults.Select(r => new
                {
                    r.Name,
                    r.RelativePath,
                    r.LineNumber,
                    r.Column,
                    r.Preview,
                    r.MatchType
                })
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