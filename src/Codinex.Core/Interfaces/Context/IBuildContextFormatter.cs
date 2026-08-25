using Codinex.Core.Models;

namespace Codinex.Core.Interfaces.Context
{
    public interface IBuildContextFormatter
    {
        string Format(BuildContext context);
    }
}