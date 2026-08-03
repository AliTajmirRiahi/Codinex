using Codinex.Infrastructure.Workspace.PromptPipeline;

namespace Codinex.Tests.Infrastructure.Workspace.PromptPipeline.PromptContextComposerTests.Base;

public abstract class PromptContextComposerTestBase
{
    protected PromptContextComposer CreateSut()
    {
        return new PromptContextComposer();
    }
}