using Codinex.Core.Models.Context;

namespace Codinex.Core.Interfaces.Context
{
    public interface IBuildContextFormatter
    {
        string Format(BuildContext context);
    }
}