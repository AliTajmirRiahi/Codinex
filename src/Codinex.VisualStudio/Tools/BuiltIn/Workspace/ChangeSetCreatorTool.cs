using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models.Tools;
using Codinex.Storage.Managers;
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
    ISourceControlStatusService sourceControlStatusService)
    : IAiTool
{

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
        var changeSet = await parser.ParseAsync(request.Arguments, cancellationToken);

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

        return outcome.Kind switch
        {
            ChangesetOutcomeKind.Applied => ToolResult.Successful(request.Id, outcome.ChangeSuccess),

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
}
