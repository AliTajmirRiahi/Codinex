using System;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.VisualStudio.Interfaces;
using EnvDTE80;
using LibGit2Sharp;

namespace Codinex.VisualStudio.SourceControl;

public interface ISourceControlStatusService
{
    Task<bool> IsSolutionUnderSourceControlAsync(CancellationToken cancellationToken);
}

[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
public sealed class SourceControlStatusService(
    IWorkspaceContext workspaceContext,
    IUiThreadDispatcher uiThreadDispatcher,
    IVisualStudioServices visualStudioServices)
    : ISourceControlStatusService
{
    public async Task<bool> IsSolutionUnderSourceControlAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await uiThreadDispatcher.SwitchToMainThreadAsync();

        if (!workspaceContext.IsSolutionOpen)
        {
            return false;
        }

        var solutionDirectory = workspaceContext.SolutionDirectory;
        var solutionPath = workspaceContext.SolutionPath;

        if (IsGitRepository(solutionDirectory))
        {
            return true;
        }

        var dte = await visualStudioServices.GetDteAsync();

        return IsItemUnderScc(dte, solutionPath) || IsItemUnderScc(dte, solutionDirectory);
    }

    private static bool IsGitRepository(string solutionDirectory)
    {
        if (string.IsNullOrWhiteSpace(solutionDirectory))
        {
            return false;
        }

        try
        {
            return Repository.Discover(solutionDirectory) != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsItemUnderScc(DTE2 dte, string path)
    {
        if (dte?.SourceControl == null || string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            return dte.SourceControl.IsItemUnderSCC(path);
        }
        catch
        {
            return false;
        }
    }
}
