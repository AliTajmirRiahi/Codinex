using System.Threading.Tasks;

namespace Codinex.Core.Interfaces.Workspace;

public interface IWorkspaceInitializer
{
    Task InitializeAsync();

    void EnsureFile(string path, string defaultContent);
}