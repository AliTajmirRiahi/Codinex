using System.Collections;
using EnvDTE;
using NSubstitute;

namespace Codify.TestCommon.Fakes.VisualStudio;

public static class FakeProjects
{
    public static Projects Create(params Project[] projects)
    {
        var collection = Substitute.For<Projects>();

        ((IEnumerable)collection)
            .GetEnumerator()
            .Returns(projects.GetEnumerator());

        return collection;
    }
}