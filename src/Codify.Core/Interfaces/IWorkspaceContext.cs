namespace Codify.Core.Interfaces;

public interface IWorkspaceContext
{
    string SolutionName { get; }

    string SolutionPath { get; }

    string SolutionDirectory { get; }

    string ActiveProjectName { get; }

    string ActiveDocumentPath { get; }

    bool IsSolutionOpen { get; }
}