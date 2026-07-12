using EnvDTE;
using EnvDTE80;
using System.Collections.Generic;
using Codify.Core.Interfaces;
using NSubstitute;

namespace Codify.TestCommon.Builders.VisualStudio;

#pragma warning disable VSTHRD010
public sealed class VisualStudioScenario
{
    internal Document? ActiveDocument { get; set; }

    internal Solution? Solution { get; set; }

    internal IReadOnlyDictionary<string, string> Files { get; set; }
        = new Dictionary<string, string>();

    public void ApplyTo(
        DTE2 dte,
        IFileSystem fileSystem)
    {
        dte.ActiveDocument.Returns(ActiveDocument);
        dte.Solution.Returns(Solution);

        foreach (var file in Files)
        {
            fileSystem.Exists(file.Key).Returns(true);
            fileSystem.ReadAllText(file.Key).Returns(file.Value);
        }
    }
}

public sealed class VisualStudioScenarioBuilder
{
    private readonly ActiveDocumentBuilder _activeDocument = new();

    private readonly SolutionBuilder _solution = new();

    private readonly Dictionary<string, string> _files = [];

    public VisualStudioScenarioBuilder WithActiveDocument(
        string filePath,
        string content)
    {
        _activeDocument.WithFile(filePath);

        _files[filePath] = content;

        return this;
    }

    public VisualStudioScenarioBuilder WithProjectFile(
        string filePath,
        string content,
        string projectName)
    {
        _solution.WithProject(

            new ProjectBuilder()

                .WithName(projectName)

                .WithItem(

                    new ProjectItemBuilder()

                        .WithFile(filePath, projectName)));

        _files[filePath] = content;

        return this;
    }

    public VisualStudioScenarioBuilder WithNonPhysicalProjectItem(
        string projectName)
    {
        _solution.WithProject(

            new ProjectBuilder()

                .WithName(projectName)

                .WithItem(

                    new ProjectItemBuilder()

                        .AsNonPhysical()));

        return this;
    }

    public VisualStudioScenario Build()
    {
        return new VisualStudioScenario
        {
            ActiveDocument = _activeDocument.Build(),
            Solution = _solution.Build(),
            Files = _files
        };
    }
}