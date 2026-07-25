using System.IO;
using System.Threading.Tasks;
using Codify.Core.Interfaces;
using Codify.Storage;

namespace Codify.Storage;

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
        if (!fileSystem.DirectoryExists(path))
        {
            fileSystem.CreateDirectory(path);
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
        if (!fileSystem.FileExists(path))
            fileSystem.WriteAllText(path, defaultContent);
    }

}
