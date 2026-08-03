using System;
using EnvDTE;
using NSubstitute;

namespace Codify.TestCommon.Fakes.VisualStudio;

#pragma warning disable VSTHRD010
public static class FakeProjectItem
{
    public const string PhysicalFileKind =
        "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}";

    public static ProjectItem Create(
        string filePath,
        string projectName)
    {
        return Create(
            filePath,
            projectName,
            null,
            null,
            PhysicalFileKind);
    }

    public static ProjectItem CreateFolder(
        ProjectItems children)
    {
        return Create(
            string.Empty,
            string.Empty,
            children,
            null,
            Guid.NewGuid().ToString());
    }

    public static ProjectItem CreateSubProject(
        Project subProject)
    {
        return Create(
            string.Empty,
            string.Empty,
            null,
            subProject,
            Guid.NewGuid().ToString());
    }

    public static ProjectItem CreateNonPhysical()
    {
        return Create(
            string.Empty,
            string.Empty,
            null,
            null,
            Guid.NewGuid().ToString());
    }

    private static ProjectItem Create(
        string filePath,
        string projectName,
        ProjectItems children,
        Project subProject,
        string kind)
    {
        var item = Substitute.For<ProjectItem>();

        item.Kind.Returns(kind);

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            item.FileNames[1].Returns(filePath);
        }

        if (!string.IsNullOrWhiteSpace(projectName))
        {
            var project = Substitute.For<Project>();

            project.Name.Returns(projectName);

            item.ContainingProject.Returns(project);
        }

        if (children != null)
        {
            item.ProjectItems.Returns(children);
        }

        if (subProject != null)
        {
            item.SubProject.Returns(subProject);
        }

        return item;
    }
}
#pragma warning restore VSTHRD010