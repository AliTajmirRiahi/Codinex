using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Core.Models.WorkspaceChanges;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.WorkspaceChanges.Validation;

public sealed class WorkspaceChangeValidator(
    IEnumerable<IWorkspaceChangeValidationRule> validationRules)
    : IWorkspaceChangeValidator
{
    public async Task<WorkspaceValidationResult> ValidateAsync(
        WorkspaceChangeSet workspaceChangeSet,
        CancellationToken cancellationToken = default)
    {
        if (workspaceChangeSet == null)
            throw new ArgumentNullException(nameof(workspaceChangeSet));

        foreach (var validationRule in validationRules)
        {
            var result = await validationRule.ValidateAsync(
                workspaceChangeSet,
                cancellationToken);

            if (!result.Success)
                return result;
        }

        return WorkspaceValidationResult.Successful();
    }
}