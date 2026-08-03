using System.Threading.Tasks;

namespace Codify.Core.Interfaces;

public interface IWorkspaceInitializer
{
    Task InitializeAsync();

    void EnsureFile(string path, string defaultContent);
}