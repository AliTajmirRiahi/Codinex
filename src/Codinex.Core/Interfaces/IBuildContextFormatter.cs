using Codinex.Core.Models;

namespace Codinex.Core.Interfaces
{
    public interface IBuildContextFormatter
    {
        string Format(BuildContext context);
    }
}