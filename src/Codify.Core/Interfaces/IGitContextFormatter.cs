using Codify.Core.Models;

namespace Codify.Core.Interfaces
{
    /// <summary>
    /// Formats Git context into prompt text.
    /// </summary>
    public interface IGitContextFormatter
    {
        string Format(GitContext context);
    }
}