using Codinex.Infrastructure.WorkspaceChanges;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.TextChangeMatcherTests.Base;

public abstract class TextChangeMatcherTestBase
{
    protected static TextChangeMatcher CreateSut()
    {
        return new TextChangeMatcher();
    }
}