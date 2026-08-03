using System.IO.Abstractions;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;

namespace Codinex.Storage.Services;

[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Foundation)]
public sealed class WorkspaceInitializer(
    IFileSystem fileSystem,
    IWorkspaceContext workspaceContext)
    : IWorkspaceInitializer
{
    public Task InitializeAsync()
    {
        EnsureDirectory(StoragePaths.Root);
        EnsureDirectory(StoragePaths.Cache);
        EnsureDirectory(StoragePaths.Sessions);
        EnsureDirectory(StoragePaths.Workspaces);

        var workspaceName = GetWorkspaceName();

        EnsureDirectory(StoragePaths.GetWorkspacePath(workspaceName));
        EnsureDirectory(StoragePaths.GetGroupsPath(workspaceName));

        EnsureFile(StoragePaths.Settings, "");
        EnsureFile(StoragePaths.Providers, "");

        EnsureFile(StoragePaths.GetWorkspaceSettingsPath(workspaceName), "");
        EnsureFile(StoragePaths.GetWorkspaceMemoryPath(workspaceName), "");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Ensures the directory exists.
    /// </summary>
    private void EnsureDirectory(string path)
    {
        if (!fileSystem.Directory.Exists(path))
        {
            fileSystem.Directory.CreateDirectory(path);
        }
    }

    /// <summary>
    /// Resolves the workspace name.
    /// </summary>
    private string GetWorkspaceName()
    {
        if (workspaceContext.IsSolutionOpen &&
            !string.IsNullOrWhiteSpace(workspaceContext.SolutionName))
        {
            return workspaceContext.SolutionName;
        }

        return "DefaultSolution";
    }

    public void EnsureFile(string path, string defaultContent)
    {
        if (!fileSystem.File.Exists(path))
            fileSystem.File.WriteAllText(path, defaultContent);
    }

}
