using Codinex.Core.Models;

namespace Codinex.Core.Interfaces
{
    /// <summary>
    /// Formats Git context into prompt text.
    /// </summary>
    public interface IGitContextFormatter
    {
        string Format(GitContext context);
    }
}