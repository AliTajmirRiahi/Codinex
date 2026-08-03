using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Helper;

namespace Codinex.Infrastructure.Helpers
{
    [AutoDiRegister(Modules.Workspace, RegistrationOrder.Foundation)]
    public class StringHelper : IStringHelper
    {
        public string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');
        }
    }
}
