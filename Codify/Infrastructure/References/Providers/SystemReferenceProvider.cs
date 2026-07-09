using Codify.Core.Abstractions;
using Codify.Core.Models;
using Codify.Infrastructure.VisualStudio.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codify.Infrastructure.References.Providers
{
    public class SystemReferenceProvider : IReferenceProvider
    {
        private readonly IVsOutputWindowService _outputWindowService;

        public SystemReferenceProvider(IVsOutputWindowService outputWindowService)
        {
            _outputWindowService = outputWindowService;
        }
        public async Task<IReadOnlyList<ReferenceItem>> GetReferencesAsync()
        {
            var panes = await _outputWindowService.GetOutputPanesAsync();

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
