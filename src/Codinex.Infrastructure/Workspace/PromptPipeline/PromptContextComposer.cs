using System.Collections.Generic;
using System.Linq;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Workspace.Prompt;

namespace Codinex.Infrastructure.Workspace.PromptPipeline
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