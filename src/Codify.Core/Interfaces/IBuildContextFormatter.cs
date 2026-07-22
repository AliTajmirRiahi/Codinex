using Codify.Core.Models;

namespace Codify.Core.Interfaces
{
    public interface IBuildContextFormatter
    {
        string Format(BuildContext context);
    }
}