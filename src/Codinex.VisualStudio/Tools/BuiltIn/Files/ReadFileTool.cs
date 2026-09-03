using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Workspace;
using Codinex.Core.Models.Tools;
using Codinex.Core.Tools;
using Codinex.VisualStudio.Interfaces;

namespace Codinex.VisualStudio.Tools.BuiltIn.Files;

/// <summary>
/// ReadFileTool
/// </summary>
[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class ReadFileTool(
    IWorkspaceFileService workspaceFileService, 
    IWorkspaceSearchService workspaceSearchService) 
    : IAiTool
{
    /// <summary>Above this many characters, a range-less read returns only the first block of lines.</summary>
    private const int MaxUntruncatedChars = 20_000;

    /// <summary>How many leading lines a range-less read of a large file returns.</summary>
    private const int TruncatedHeadLines = 400;

    public string Name => "read_file";

    public string Description => "Use only when the requested information cannot be obtained from the current workspace context.";

    public IReadOnlyList<string> Capabilities =>
    [
        "read file",
        "open file contents",
        "show file contents",
        "inspect file",
        "view file"
    ];

    public string StatusMessage => "Reading file...";

    public ToolVisibility Visibility { get; } = ToolVisibility.Model;


    public ToolDefinition Definition { get; } =
        new ToolDefinition(
            new Dictionary<string, ToolProperty>
            {
                ["path"] = new ToolProperty(
                     ToolPropertyType.String,
                    "Reads a file from the current workspace.\r\n\r\nUse this tool ONLY when the file path is known.\r\nDo NOT call this tool if you don't know the file path. A range-less read of a large file returns only its first block of lines; use startLine/endLine to page through the rest."),

                ["startLine"] = new ToolProperty(
                    ToolPropertyType.Integer,
                    "Optional. 1-based first line to return. Combine with endLine to read a slice of a large file."),

                ["endLine"] = new ToolProperty(
                    ToolPropertyType.Integer,
                    "Optional. 1-based last line to return, inclusive. Omit to read to the end of the file.")
            },
            [
                "path"
            ]);

    public async Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Yield();

        var query = request.GetRequiredString("path");

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

        var content = await workspaceFileService.ReadAsync(file.FullPath, cancellationToken)
                      ?? string.Empty;

        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var totalLines = lines.Length;

        var startLine = request.GetInt32("startLine");
        var endLine = request.GetInt32("endLine");
        var hasRange = startLine > 0 || endLine > 0;

        string outContent;
        int fromLine;
        int toLine;
        string note = null;

        if (hasRange)
        {
            fromLine = startLine > 0 ? startLine : 1;

            if (fromLine > totalLines)
            {
                return ToolResult.Successful(
                    request.Id,
                    new
                    {
                        file.Name,
                        file.RelativePath,
                        TotalLines = totalLines,
                        StartLine = 0,
                        EndLine = 0,
                        IsTruncated = false,
                        Note = $"startLine ({fromLine}) is past the end of the file ({totalLines} lines).",
                        Content = string.Empty
                    });
            }

            toLine = endLine > 0 ? endLine : totalLines;
            toLine = Math.Min(totalLines, Math.Max(toLine, fromLine));

            outContent = string.Join("\n", lines.Skip(fromLine - 1).Take(toLine - fromLine + 1));
        }
        else if (content.Length > MaxUntruncatedChars)
        {
            fromLine = 1;

            // Cap the head by both line count and characters so the result never trips the
            // conversation engine's own (blunter) tool-result truncation.
            var head = new List<string>();
            var budget = MaxUntruncatedChars;

            foreach (var lineText in lines)
            {
                if (head.Count >= TruncatedHeadLines || budget - lineText.Length - 1 < 0)
                {
                    break;
                }

                head.Add(lineText);
                budget -= lineText.Length + 1;
            }

            if (head.Count == 0)
            {
                head.Add(lines[0]);
            }

            toLine = head.Count;
            outContent = string.Join("\n", head);

            note = $"File is large ({totalLines} lines, {content.Length:N0} chars). Showing lines 1-{toLine}. " +
                   "Call read_file again with startLine/endLine for the rest, or use get_file_elements/read_element for a source file.";
        }
        else
        {
            fromLine = 1;
            toLine = totalLines;

            // Whole small file: return it byte-for-byte so change_set_creator Search text stays exact.
            outContent = content;
        }

        return ToolResult.Successful(
            request.Id,
            new
            {
                file.Name,
                file.RelativePath,
                TotalLines = totalLines,
                StartLine = fromLine,
                EndLine = toLine,
                IsTruncated = toLine < totalLines,
                Note = note,
                Content = outContent
            });
    }
}
