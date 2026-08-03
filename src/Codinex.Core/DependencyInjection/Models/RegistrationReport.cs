using System.Collections.Generic;

namespace Codinex.Core.DependencyInjection.Models
{
    public sealed class RegistrationReport
    {
        public List<RegistrationItem> Items { get; } = [];
    }
}
