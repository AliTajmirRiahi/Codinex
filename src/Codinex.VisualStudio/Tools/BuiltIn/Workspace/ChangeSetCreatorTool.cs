using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Workspace;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models.Tools;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Storage.Managers;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.SourceControl;
using Codinex.Core.Tools;
using Codinex.VisualStudio.Tools.BuiltIn.Workspace.Schemas;

namespace Codinex.VisualStudio.Tools.BuiltIn.Workspace;

[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class ChangeSetCreatorTool(
    IWorkspaceChangeParser parser,
    IWorkspaceChangeValidator validator,
    IEditFileChangeResolver changeResolver,
    IChangesetSessionService changesetSessionService,
    SettingsManager settingsManager,
    ISourceControlStatusService sourceControlStatusService,
    IWorkspaceFileService workspaceFileService,
    IWorkspaceSearchService workspaceSearchService)
    : IAiTool
{
    private const int RegionContextLines = 4;
    private const int MaxRegionChars = 2_000;

    public string Name => "change_set_creator";

    public string Description =>
        "Create a workspace change set describing all file and directory modifications.\n\n" +
        "Return every requested modification in a single change set. " +
        "Do not explain the changes. Return only the structured change data." +
        @"The Search text must be copied exactly from the source file. Do not append \n, \r, or trailing whitespace unless they are intentionally part of the selected text" +
        "If a tool returns status = completed, treat the user's request as completed unless additional tool calls are required for a different task";

    public IReadOnlyList<string> Capabilities =>
    [
        "apply changes",
        "edit file",
        "modify code",
        "create file",
        "delete file",
        "rename file",
        "move file",
        "create directory",
        "delete directory",
        "workspace changes",
        "change set"
    ];

    public ToolVisibility Visibility => ToolVisibility.Model;

    public string StatusMessage => "Applying workspace changes...";

    public ToolDefinition Definition => new(
        new Dictionary<string, ToolProperty>
        {
            ["changes"] = WorkspaceToolSchemasFlat.WorkspaceChangeSetProp
        },
        ["changes"],
        true);


    public async Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken)
    {
        var parseResult = await parser.ParseAsync(request.Arguments, cancellationToken);

        if (!parseResult.Success)
        {
            return ToolResult.Failed(request.Id, parseResult.Errors);
        }

        var changeSet = parseResult.ChangeSet;

        var validationResult = await validator.ValidateAsync(
            changeSet,
            cancellationToken);

        if (!validationResult.Success)
        {
            return ToolResult.Failed(request.Id, validationResult.Errors);
        }

        var resolutionResult = await changeResolver.ResolveAsync(
            changeSet,
            cancellationToken);

        if (!resolutionResult.Success)
        {
            return ToolResult.Failed(request.Id, resolutionResult.Errors);
        }

        var bypassPreview = settingsManager.Settings.ByPassPreviewChangeAndApplyChangeDirectly &&
                            await sourceControlStatusService.IsSolutionUnderSourceControlAsync(cancellationToken);

        var outcome = bypassPreview
            ? await changesetSessionService.ApplyDirectAsync(changeSet, resolutionResult)
            : await changesetSessionService.RunReviewAsync(changeSet, resolutionResult, cancellationToken);

        if (outcome.Kind == ChangesetOutcomeKind.Applied)
        {
            await EnrichAppliedRegionsAsync(outcome.ChangeSuccess, changeSet, cancellationToken);

            return ToolResult.Successful(request.Id, outcome.ChangeSuccess);
        }

        return outcome.Kind switch
        {

            ChangesetOutcomeKind.Rejected => ToolResult.Failed(
                request.Id,
                outcome.Message +
                " Do not tell the user the change was made. Tell the user the change was rejected and not applied."),

            ChangesetOutcomeKind.Undecided => ToolResult.Failed(
                request.Id,
                "The user has not decided yet on the proposed changes; the review is still pending in the " +
                "Code Changes window. Do not tell the user the change was made or rejected. Ask them to " +
                "complete the review, or continue with other unrelated tasks in the meantime."),

            _ => ToolResult.Failed(request.Id, outcome.Message, outcome.Error)
        };
    }

    /// <summary>
    /// Fills <see cref="ChangedFileResult.AppliedRegion"/> for each edited/created file so the
    /// model can see what actually landed instead of re-reading the file. Best-effort: any
    /// failure just leaves the region unset.
    /// </summary>
    private async Task EnrichAppliedRegionsAsync(
        WorkspaceChangeSuccess success,
        WorkspaceChangeSet changeSet,
        CancellationToken cancellationToken)
    {
        if (success?.Files == null || changeSet?.Changes == null)
        {
            return;
        }

        foreach (var file in success.Files)
        {
            if (!string.Equals(file.Status, "success", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                file.AppliedRegion = file.Operation switch
                {
                    "EditFile" => await BuildEditRegionAsync(file.Path, changeSet, cancellationToken),
                    "CreateFile" => BuildCreateRegion(file.Path, changeSet),
                    _ => null
                };
            }
            catch
            {
                // Region echoing is a convenience, never a failure reason.
            }
        }
    }

    private async Task<string> BuildEditRegionAsync(
        string path,
        WorkspaceChangeSet changeSet,
        CancellationToken cancellationToken)
    {
        var change = changeSet.Changes
            .OfType<EditFileChange>()
            .FirstOrDefault(c => PathsMatch(c.FilePath, path));

        if (change?.TextChanges == null || change.TextChanges.Count == 0)
        {
            return null;
        }

        var resolved = workspaceSearchService.FindFiles(change.FilePath).FirstOrDefault();

        if (resolved == null)
        {
            return null;
        }

        var fileText = await workspaceFileService.ReadAsync(resolved.FullPath, cancellationToken);

        if (string.IsNullOrEmpty(fileText))
        {
            return null;
        }

        var normalized = fileText.Replace("\r\n", "\n");
        var lines = normalized.Split('\n');
        var wanted = new SortedSet<int>();

        foreach (var textChange in change.TextChanges)
        {
            var needle = string.IsNullOrEmpty(textChange.Replace)
                ? textChange.Search
                : textChange.Replace;

            if (string.IsNullOrEmpty(needle))
            {
                continue;
            }

            var index = normalized.IndexOf(needle.Replace("\r\n", "\n"), StringComparison.Ordinal);

            if (index < 0)
            {
                continue;
            }

            var startLine = CountNewlines(normalized, index);
            var endLine = CountNewlines(normalized, index + needle.Length);

            for (var line = Math.Max(0, startLine - RegionContextLines);
                 line <= Math.Min(lines.Length - 1, endLine + RegionContextLines);
                 line++)
            {
                wanted.Add(line);
            }
        }

        if (wanted.Count == 0)
        {
            return null;
        }

        var sb = new StringBuilder();
        int? previous = null;

        foreach (var line in wanted)
        {
            if (previous.HasValue && line > previous.Value + 1)
            {
                sb.Append("    ...\n");
            }

            sb.Append(lines[line]).Append('\n');
            previous = line;

            if (sb.Length >= MaxRegionChars)
            {
                sb.Append("    ...(truncated)\n");
                break;
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string BuildCreateRegion(string path, WorkspaceChangeSet changeSet)
    {
        var change = changeSet.Changes
            .OfType<CreateFileChange>()
            .FirstOrDefault(c => PathsMatch(c.FilePath, path));

        var content = change?.Content;

        if (string.IsNullOrEmpty(content))
        {
            return null;
        }

        return content.Length <= MaxRegionChars
            ? content
            : content.Substring(0, MaxRegionChars) + "\n    ...(truncated)";
    }

    private static bool PathsMatch(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
        {
            return false;
        }

        var na = a.Replace('\\', '/').TrimStart('/');
        var nb = b.Replace('\\', '/').TrimStart('/');

        return na.EndsWith(nb, StringComparison.OrdinalIgnoreCase)
            || nb.EndsWith(na, StringComparison.OrdinalIgnoreCase);
    }

    private static int CountNewlines(string text, int upToIndex)
    {
        var count = 0;

        for (var i = 0; i < upToIndex && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                count++;
            }
        }

        return count;
    }
}
