using Codify.Core.Models;

namespace Codify.Core.Interfaces
{
    /// <summary>
    /// Formats project context into prompt text.
    /// </summary>
    public interface IProjectContextFormatter
    {
        string Format(ProjectContext context);
    }
}