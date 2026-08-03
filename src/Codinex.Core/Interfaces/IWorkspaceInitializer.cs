using System.Threading.Tasks;

namespace Codinex.Core.Interfaces;

public interface IWorkspaceInitializer
{
    Task InitializeAsync();

    void EnsureFile(string path, string defaultContent);
}