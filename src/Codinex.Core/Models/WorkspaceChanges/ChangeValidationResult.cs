using System;
using System.Collections.Generic;

namespace Codinex.Core.Models.WorkspaceChanges;

/// <summary>
/// The output of Find + Validate + Plan for a workspace change set's EditFileChanges: on
/// success, the resolved plan (<see cref="Changes"/>) that Viewer and Applier consume.
/// </summary>
public sealed class ChangeValidationResult
{
    public bool Success { get; set; }

    public IReadOnlyList<ResolvedFileChange> Changes { get; set; } = [];

    public IReadOnlyList<ChangeValidationError> Errors { get; set; } = [];

    public static ChangeValidationResult Successful(IReadOnlyList<ResolvedFileChange> changes)
    {
        return new ChangeValidationResult
        {
            Success = true,
            Changes = changes
        };
    }

    public static ChangeValidationResult Failed(IReadOnlyList<ChangeValidationError> errors)
    {
        if (errors == null)
            throw new ArgumentNullException(nameof(errors));

        return new ChangeValidationResult
        {
            Success = false,
            Errors = errors
        };
    }

    public static ChangeValidationResult Failed(ChangeValidationError error)
    {
        return error == null ? throw new ArgumentNullException(nameof(error)) : Failed([error]);
    }
}
