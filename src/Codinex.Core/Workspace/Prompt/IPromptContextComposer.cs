using System.Collections.Generic;

namespace Codify.Core.Workspace.Prompt
{
    /// <summary>
    /// Composes the final prompt context from provider results.
    /// </summary>
    public interface IPromptContextComposer
    {
        PromptContext Compose(
            IReadOnlyList<ContextProviderResult> providerResults);
    }
}