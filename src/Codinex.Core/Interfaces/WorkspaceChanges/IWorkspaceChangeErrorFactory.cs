using Codify.Core.Models.WorkspaceChanges;
using System;

namespace Codify.Core.Interfaces.WorkspaceChanges;

public interface IWorkspaceChangeErrorFactory
{
    WorkspaceChangeError Create(
        WorkspaceChangeErrorCode code,
        string filePath,
        Guid changeId,
        string message);
}