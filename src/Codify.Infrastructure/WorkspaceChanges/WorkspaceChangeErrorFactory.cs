using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Core.Models.WorkspaceChanges;
using System;

namespace Codify.Infrastructure.WorkspaceChanges;

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