using Codinex.Core.Models;

namespace Codinex.Core.Interfaces
{
    /// <summary>
    /// Formats project context into prompt text.
    /// </summary>
    public interface IProjectContextFormatter
    {
        string Format(ProjectContext context);
    }
}