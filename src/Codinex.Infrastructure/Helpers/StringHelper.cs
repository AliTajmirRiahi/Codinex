using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces.Helper;

namespace Codify.Infrastructure.Helpers
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
