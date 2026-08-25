using Codinex.Core.Models.Context;

namespace Codinex.Core.Interfaces.Context
{
    /// <summary>
    /// Formats project context into prompt text.
    /// </summary>
    public interface IProjectContextFormatter
    {
        string Format(ProjectContext context);
    }
}