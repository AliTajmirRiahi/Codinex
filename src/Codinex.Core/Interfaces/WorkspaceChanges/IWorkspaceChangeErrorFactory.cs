using System;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Core.Interfaces.WorkspaceChanges;

public interface IWorkspaceChangeErrorFactory
{
    WorkspaceChangeError Create(
        WorkspaceChangeErrorCode code,
        string filePath,
        Guid changeId,
        string message);
}