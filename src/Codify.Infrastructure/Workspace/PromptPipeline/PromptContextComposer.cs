using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Workspace.Prompt;
using System.Collections.Generic;
using System.Linq;

namespace Codify.Infrastructure.Workspace.PromptPipeline
{
    /// <summary>
    /// Composes the final prompt context from provider results.
    /// </summary>
    [AutoDiRegister(Modules.Prompt, RegistrationOrder.Infrastructure)]
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