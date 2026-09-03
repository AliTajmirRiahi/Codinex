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

    /// <summary>
    /// Cumulative cap on preview text across all returned rows. Individual previews are
    /// already bounded by the search service; this is a second backstop so a single
    /// response can never balloon the conversation regardless of row count.
    /// </summary>
    private const int MaxTotalPreviewChars = 8_000;

    private static readonly char[] GlobChars = ['*', '?'];

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

            string note = null;

            // "pattern" with no wildcard characters is almost always a text search the model
            // mislabeled - FindByPattern is a filesystem glob and matches nothing for a plain
            // string. Fall back to a text search rather than returning an empty result.
            if (results.Count == 0
                && searchType == SearchProjectType.Pattern
                && query.IndexOfAny(GlobChars) < 0)
            {
                var textResults = workspaceSearchService.Search(query, SearchProjectType.Text);

                if (textResults.Count > 0)
                {
                    results = textResults;
                    type = SearchProjectType.Text.ToString().ToLowerInvariant();
                    note = "The query has no wildcard characters, so it was run as a 'text' search instead of 'pattern'.";
                }
            }

            var totalCount = results.Count;

            if (totalCount > 0 && skip >= totalCount)
            {
                note = string.IsNullOrEmpty(note)
                    ? $"skip ({skip}) is past the last result - there are only {totalCount}. Call again with skip:0 to see them."
                    : note + $" Also: skip ({skip}) is past the only {totalCount} result(s); use skip:0.";
            }

            var limitedResults = results
                .Skip(skip)
                .Take(take)
                .ToList();

            var previewBudget = MaxTotalPreviewChars;

            var projectedResults = new List<object>(limitedResults.Count);

            foreach (var r in limitedResults)
            {
                var preview = r.Preview ?? string.Empty;

                if (previewBudget <= 0)
                {
                    preview = string.Empty;
                }
                else if (preview.Length > previewBudget)
                {
                    preview = preview.Substring(0, previewBudget) + "…";
                    previewBudget = 0;
                }
                else
                {
                    previewBudget -= preview.Length;
                }

                projectedResults.Add(new
                {
                    r.Name,
                    r.RelativePath,
                    r.LineNumber,
                    r.Column,
                    Preview = preview,
                    r.MatchType
                });
            }

            // FullPath is intentionally omitted: it repeats the same workspace-root prefix on
            // every row and is redundant with RelativePath, which is all downstream tools need.
            var data = new
            {
                Query = query,
                Type = type,
                TotalCount = totalCount,
                ReturnedCount = limitedResults.Count,
                IsTruncated = skip + limitedResults.Count < totalCount,
                Note = note,
                Results = projectedResults
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