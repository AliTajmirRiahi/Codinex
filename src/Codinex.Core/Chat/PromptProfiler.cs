using System.Collections.Generic;
using System.Linq;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Chat;
using Codinex.Core.Models;
using Codinex.Core.Workspace.Prompt;

namespace Codinex.Core.Chat
{
    [AutoDiRegister(Modules.Prompt, RegistrationOrder.Platform)]
    public sealed class PromptProfiler : IPromptProfiler
    {
        public PromptProfileResult Profile(PromptContext context)
        {
            var sections = (context?.Sections ?? new List<PromptContextSection>())
                .Select(ProfileSection)
                .Where(x => x.Characters > 0)
                .ToList();
            var totalCharacters = sections.Sum(x => x.Characters);

            ApplySectionPercentages(sections, totalCharacters);

            return new PromptProfileResult
            {
                Sections = sections,
                TotalCharacters = totalCharacters,
                EstimatedTokens = EstimateTokens(totalCharacters)
            };
        }

        private static PromptSectionProfile ProfileSection(PromptContextSection section)
        {
            var children = (section.Items ?? new List<PromptContextItem>())
                .Select(ProfileItem)
                .Where(x => !string.IsNullOrWhiteSpace(x.Name) && x.Characters > 0)
                .ToList();
            var characters = children.Count > 0
                ? children.Sum(x => x.Characters)
                : CountCharacters(section);

            return new PromptSectionProfile
            {
                Name = section.Name,
                Characters = characters,
                EstimatedTokens = EstimateTokens(characters),
                Children = children
            };
        }

        private static PromptSectionProfile ProfileItem(PromptContextItem item)
        {
            var characters = CountCharacters(item);

            return new PromptSectionProfile
            {
                Name = item.Title,
                Characters = characters,
                EstimatedTokens = EstimateTokens(characters),
                Reason = item.Reason
            };
        }

        private static void ApplySectionPercentages(IEnumerable<PromptSectionProfile> sections, int totalCharacters)
        {
            foreach (var section in sections ?? Enumerable.Empty<PromptSectionProfile>())
            {
                section.SectionPercentage = totalCharacters == 0
                    ? 0
                    : (double)section.Characters / totalCharacters * 100;

                ApplySectionPercentages(section.Children, totalCharacters);
            }
        }

        private static int CountCharacters(PromptContextSection section)
        {
            if (section == null)
            {
                return 0;
            }

            return (section.Items ?? new List<PromptContextItem>())
                .Sum(CountCharacters);
        }

        private static int CountCharacters(PromptContextItem item)
        {
            if (item == null)
            {
                return 0;
            }

            var total = 0;

            if (!string.IsNullOrWhiteSpace(item.Title))
            {
                total += item.Title.Length;
            }

            if (!string.IsNullOrWhiteSpace(item.Content))
            {
                total += item.Content.Length;
            }

            return total;
        }

        private static int EstimateTokens(int characters)
        {
            return characters / 4;
        }
    }
}
