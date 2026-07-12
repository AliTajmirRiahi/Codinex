using Codify.TestCommon.Fakes.VisualStudio;
using EnvDTE;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Codify.TestCommon.Builders.VisualStudio;

#pragma warning disable VSTHRD010
public sealed class ProjectItemBuilder
{
    private readonly List<ProjectItem> _children = [];

    private string _filePath = string.Empty;
    private string _projectName = string.Empty;
    private bool _isPhysical = true;

    public ProjectItemBuilder WithFile(
        string filePath,
        string projectName)
    {
        _filePath = filePath;
        _projectName = projectName;
        return this;
    }

    public ProjectItemBuilder AsNonPhysical()
    {
        _isPhysical = false;
        return this;
    }

    public ProjectItemBuilder WithChild(ProjectItemBuilder builder)
    {
        _children.Add(builder.Build());
        return this;
    }

    public ProjectItem Build()
    {
        var item = Substitute.For<ProjectItem>();

        item.Kind.Returns(
            _isPhysical
                ? "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}"
                : Guid.NewGuid().ToString());

        item.FileNames[1].Returns(_filePath);

        var project = Substitute.For<Project>();
        project.Name.Returns(_projectName);

        item.ContainingProject.Returns(project);

        if (_children.Any())
        {
            item.ProjectItems.Returns(
                FakeProjectItems.Create(_children.ToArray()));
        }

        return item;
    }
}