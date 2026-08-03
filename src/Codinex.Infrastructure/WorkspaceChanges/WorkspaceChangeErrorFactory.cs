using System;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Infrastructure.WorkspaceChanges;

[AutoDiRegister(Modules.MissionEngine, RegistrationOrder.Features)]
public sealed class WorkspaceChangeErrorFactory
    : IWorkspaceChangeErrorFactory
{
    public WorkspaceChangeError Create(
        WorkspaceChangeErrorCode code,
        string filePath,
        Guid changeId,
        string message)
    {
        return new WorkspaceChangeError
        {
            Code = code,
            FilePath = filePath,
            ChangeId = changeId,
            Message = message
        };
    }
}