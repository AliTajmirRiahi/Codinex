using System.Collections.Generic;
using System.Linq;
using Codify.TestCommon.Fakes.VisualStudio;
using EnvDTE;
using NSubstitute;

namespace Codify.TestCommon.Builders.VisualStudio;

#pragma warning disable VSTHRD010
public sealed class ProjectBuilder
{
    private readonly List<ProjectItemBuilder> _items = [];

    private string _name = string.Empty;

    public ProjectBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ProjectBuilder WithItem(ProjectItemBuilder builder)
    {
        _items.Add(builder);
        return this;
    }

    public Project Build()
    {
        var project = Substitute.For<Project>();

        project.Name.Returns(_name);

        project.ProjectItems.Returns(
            FakeProjectItems.Create(
                _items.Select(x => x.Build()).ToArray()));

        return project;
    }
}