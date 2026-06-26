using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Codify.Core.Abstractions;
using Codify.Core.Models;

namespace Codify.Infrastructure.References
{
    public class ReferenceManager
    {
        private readonly IReadOnlyList<IReferenceProvider> _providers;

        public ReferenceManager(IEnumerable<IReferenceProvider> providers)
        {
            _providers = providers.ToList();
        }

        public async Task<IReadOnlyList<ReferenceItem>> GetAllReferencesAsync()
        {
            var tasks = _providers.Select(provider => provider.GetReferencesAsync());
            var results = await Task.WhenAll(tasks);

            return results
                .Where(items => items != null)
                .SelectMany(items => items)
                .ToList();
        }
    }
}
