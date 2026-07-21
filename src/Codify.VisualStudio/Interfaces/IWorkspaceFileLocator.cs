using System.Collections.Generic;
using Codify.VisualStudio.Models;

namespace Codify.VisualStudio.Interfaces
{
    public interface IWorkspaceFileLocator
    {
        IReadOnlyList<WorkspaceFile> Find(string query);
    }
}
