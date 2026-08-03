using Codify.Infrastructure.WorkspaceChanges;

namespace Codify.Tests.Infrastructure.WorkspaceChanges.TextChangeMatcherTests.Base;

public abstract class TextChangeMatcherTestBase
{
    protected static TextChangeMatcher CreateSut()
    {
        return new TextChangeMatcher();
    }
}