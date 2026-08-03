using EnvDTE;
using NSubstitute;

namespace Codify.TestCommon.Fakes.VisualStudio;

#pragma warning disable VSTHRD010
public static class FakeProject
{
    public static Project Create(
        string name,
        ProjectItems projectItems)
    {
        var project = Substitute.For<Project>();

        project.Name.Returns(name);
        project.ProjectItems.Returns(projectItems);

        return project;
    }
}
#pragma warning restore VSTHRD010