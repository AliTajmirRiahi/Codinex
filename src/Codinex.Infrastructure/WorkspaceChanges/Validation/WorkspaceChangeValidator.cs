using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Infrastructure.WorkspaceChanges.Validation;

[AutoDiRegister(Modules.MissionEngine, RegistrationOrder.Features)]
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