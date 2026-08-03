using System;
using System.Collections.Generic;

namespace Codinex.Core.Models.WorkspaceChanges;

public sealed class WorkspaceValidationResult
{
    public bool Success => Errors.Count == 0;

    public List<WorkspaceValidationError> Errors { get; } = [];

    public static WorkspaceValidationResult Successful()
    {
        return new WorkspaceValidationResult();
    }

    public static WorkspaceValidationResult Failed(
        IEnumerable<WorkspaceValidationError> errors)
    {
        if (errors == null)
            throw new ArgumentNullException(nameof(errors));

        var result = new WorkspaceValidationResult();

        result.Errors.AddRange(errors);

        return result;
    }

    public static WorkspaceValidationResult Failed(
        WorkspaceValidationError error)
    {
        return error == null ? throw new ArgumentNullException(nameof(error)) : Failed([error]);
    }

    public void AddError(WorkspaceValidationError error)
    {
        if (error == null)
            throw new ArgumentNullException(nameof(error));

        Errors.Add(error);
    }

    public void AddErrors(IEnumerable<WorkspaceValidationError> errors)
    {
        if (errors == null)
            throw new ArgumentNullException(nameof(errors));

        Errors.AddRange(errors);
    }
}