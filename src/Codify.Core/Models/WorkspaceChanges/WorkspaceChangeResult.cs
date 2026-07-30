using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codify.Core.Models.WorkspaceChanges;

public sealed class WorkspaceChangeResult
{
    public bool Success { get; set; }

    public WorkspaceChangeError Error { get; set; }

    public static WorkspaceChangeResult Successful()
    {
        return new WorkspaceChangeResult
        {
            Success = true
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
