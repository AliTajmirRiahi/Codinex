using Codinex.Core.Models.Context;

namespace Codinex.Core.Interfaces.Context
{
    /// <summary>
    /// Formats Git context into prompt text.
    /// </summary>
    public interface IGitContextFormatter
    {
        string Format(GitContext context);
    }
}