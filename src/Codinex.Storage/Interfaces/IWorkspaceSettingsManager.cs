using System.Threading.Tasks;
using Codinex.Storage.Models;

namespace Codinex.Storage.Interfaces
{
    public interface IWorkspaceSettingsManager
    {
        WorkspaceSettings Settings { get; }

        Task InitializeAsync();

        Task SaveAsync(WorkspaceSettings newSettings);
    }
}
