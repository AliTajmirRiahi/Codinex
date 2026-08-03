using EnvDTE;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;
using Codify.TestCommon.Fakes.VisualStudio;

namespace Codify.TestCommon.Builders.VisualStudio;

#pragma warning disable VSTHRD010
public sealed class SolutionBuilder
{
    private readonly List<ProjectBuilder> _projects = [];

    public SolutionBuilder WithProject(ProjectBuilder builder)
    {
        _projects.Add(builder);
        return this;
    }

    public Solution Build()
    {
        var solution = Substitute.For<Solution>();

        solution.IsOpen.Returns(true);

        solution.Projects.Returns(
            FakeProjects.Create(
                _projects
                    .Select(x => x.Build())
                    .ToArray()));

        return solution;
    }
}