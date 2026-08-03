using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codify.Core.Models.Tools;

namespace Codify.Core.Models.WorkspaceChanges;

public sealed class WorkspaceChangeResult
{
    public bool Success { get; set; }

    public WorkspaceChangeError Error { get; set; }

    public WorkspaceChangeSuccess ChangeSuccess { get; set; }

    public static WorkspaceChangeResult Successful(WorkspaceChangeSuccess success = null)
    {
        return new WorkspaceChangeResult
        {
            Success = true,
            ChangeSuccess = success,
        };
    }

    public static WorkspaceChangeResult Failed(
        WorkspaceChangeError error)
    {
        return new WorkspaceChangeResult
        {
            Success = false,
            Error = error
        };
    }
}
