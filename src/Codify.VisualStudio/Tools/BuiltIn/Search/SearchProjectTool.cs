using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Conversation;
using Codify.Core.Models;
using Codify.Core.Tools;

namespace Codify.VisualStudio.Tools.BuiltIn.Search;

/// <summary>
/// SearchProjectTool
/// </summary>
public sealed class SearchProjectTool : IAiTool
{
    public string Name => "search_project";

    public string Description =>
        "Searches the current workspace for " +
        "files, folders, text, symbols, " +
        "or other project items matching" +
        " the given query. Use this tool" +
        " when the location of " +
        "the requested information is unknown.";
    public ToolVisibility Visibility { get; } = ToolVisibility.Model;

    public ToolDefinition Definition => new(
        new Dictionary<string, ToolProperty>
        {
            ["query"] = new(
                ToolPropertyType.String,
                "The text, symbol, filename, or pattern to search for."),

            ["maxResults"] = new(
                ToolPropertyType.Integer,
                "Maximum number of results to return. Default is 20.")
        },
        ["query"]);



    public Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
