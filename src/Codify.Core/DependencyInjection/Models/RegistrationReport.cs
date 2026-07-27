using System.Collections.Generic;

namespace Codify.Core.DependencyInjection.Models
{
    public sealed class RegistrationReport
    {
        public List<RegistrationItem> Items { get; } = [];
    }
}
