using Codinex.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models.Tools;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Core.Tools;
using Codinex.VisualStudio.Tools.BuiltIn.Workspace.Schemas;

namespace Codinex.VisualStudio.Tools.BuiltIn.Workspace;

[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class ChangeSetCreatorTool(
    IWorkspaceChangeParser parser,
    IWorkspaceChangeValidator validator,
    IWorkspacePreviewService previewService,
    IWorkspaceApprovalService approvalService,
    IWorkspaceChangeApplier applier)
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

        var changesetId = await previewService.ShowAsync(changeSet, cancellationToken);

        var decision = await approvalService.WaitForApprovalAsync(changesetId, cancellationToken);

        var approvedChanges = changeSet.Changes
            .Where(change => decision.FileDecisions.TryGetValue(WorkspaceChangePathResolver.GetPath(change), out var approved) && approved)
            .ToList();

        var rejectedChanges = changeSet.Changes
            .Except(approvedChanges)
            .ToList();

        if (approvedChanges.Count == 0)
        {
            return ToolResult.Failed(
                request.Id,
                "The user rejected all proposed changes in the review window." +
                (string.IsNullOrWhiteSpace(decision.Reason) ? string.Empty : $" Rejection reason: {decision.Reason}") +
                " No files were created, edited, deleted, renamed, or moved. " +
                "Do not tell the user the change was made. Tell the user the change was rejected and not applied.");
        }

        var result = await applier.ApplyAsync(
             new WorkspaceChangeSet { Changes = approvedChanges },
             cancellationToken);

        if (!result.Success)
        {
            return ToolResult.Failed(request.Id, result.Error.Message, result.Error);
        }

        if (rejectedChanges.Count > 0)
        {
            var rejectedPaths = rejectedChanges
                .Select(WorkspaceChangePathResolver.GetPath)
                .Distinct()
                .ToList();

            result.ChangeSuccess.Message =
                $"{result.ChangeSuccess.Message} The user rejected these and they were NOT applied: {string.Join(", ", rejectedPaths)}." +
                (string.IsNullOrWhiteSpace(decision.Reason) ? string.Empty : $" Rejection reason: {decision.Reason}") +
                " Do not tell the user the rejected changes were made.";
        }

        return ToolResult.Successful(request.Id, result.ChangeSuccess);
    }
}