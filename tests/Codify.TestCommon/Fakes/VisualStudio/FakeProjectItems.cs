using EnvDTE;
using NSubstitute;
using System;
using System.Collections;

namespace Codify.TestCommon.Fakes.VisualStudio;

#pragma warning disable VSTHRD010
public static class FakeProjectItems
{
    public static ProjectItems Create(params ProjectItem[] items)
    {
        var collection = Substitute.For<ProjectItems>();

        collection.Count.Returns(items.Length);

        collection.Item(Arg.Any<object>())
            .Returns(call =>
            {
                var index = Convert.ToInt32(call[0]);
                return items[index - 1];
            });

        ((IEnumerable)collection)
            .GetEnumerator()
            .Returns(_ => ((IEnumerable)items).GetEnumerator());

        return collection;
    }
}
#pragma warning restore VSTHRD010