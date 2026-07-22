using System.Collections.Generic;
using System.Linq;
using Codify.Core.Workspace.Prompt;

namespace Codify.Infrastructure.Workspace.PromptPipeline
{
    /// <summary>
    /// Composes the final prompt context from provider results.
    /// </summary>
    public sealed class PromptContextComposer : IPromptContextComposer
    {
        public PromptContext Compose(
            IReadOnlyList<ContextProviderResult> providerResults)
        {
            var context = new PromptContext();

            var section = new PromptContextSection
            {
                Name = "Workspace"
            };

            foreach (var result in providerResults.Where(r => !r.IsEmpty))
            {
                foreach (var item in result.Items)
                {
                    section.Items.Add(item);
                }
            }

            if (section.Items.Count > 0)
            {
                context.Sections.Add(section);
            }

            return context;
        }
    }
}