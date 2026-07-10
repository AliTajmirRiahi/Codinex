using Codify.Core.Interfaces;
using Codify.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Codify.VisualStudio.Interfaces;

namespace Codify.VisualStudio.References.Providers
{
    public class SystemReferenceProvider(IVsOutputWindowService outputWindowService) : IReferenceProvider
    {
        public async Task<IReadOnlyList<ReferenceItem>> GetReferencesAsync()
        {
            var panes = await outputWindowService.GetOutputPanesAsync();

            var items = panes.Select(pane => new ReferenceItem
                {
                    Id = $"output:{pane.Name}",
                    Name = pane.Name,
                    Description = "Visual Studio Output Window",
                    Type = ReferenceKind.Output,
                    Icon = "fileTypes/icon-build",
                    Value = $"output:{pane.Name}"
                })
                .ToList();

            return items;
        }
    }
}
