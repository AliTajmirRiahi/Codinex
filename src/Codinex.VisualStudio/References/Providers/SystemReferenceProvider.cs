using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.References;
using Codinex.Core.Models.References;
using Codinex.VisualStudio.Interfaces;

namespace Codinex.VisualStudio.References.Providers
{
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
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
