using System.Collections.Generic;

namespace Codify.Infrastructure.DependencyInjection.Models
{
    public sealed class RegistrationReport
    {
        public IReadOnlyList<RegistrationItem> Items { get; }
    }
}
