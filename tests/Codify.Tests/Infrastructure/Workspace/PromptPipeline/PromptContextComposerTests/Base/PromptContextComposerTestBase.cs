using Codify.Infrastructure.Workspace.PromptPipeline;

namespace Codify.Tests.Infrastructure.Workspace.PromptPipeline.PromptContextComposerTests.Base;

public abstract class PromptContextComposerTestBase
{
    protected PromptContextComposer CreateSut()
    {
        return new PromptContextComposer();
    }
}