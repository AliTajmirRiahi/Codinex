using System.Collections.Generic;
using System.Threading.Tasks;

namespace Codify.Storage
{
    /// <summary>
    /// Generic storage abstraction used by Codify managers.
    /// </summary>
    public interface IStorageService
    {
        Task SaveAsync<T>(string path, T data);

        Task<T> LoadAsync<T>(string path);

        Task<bool> ExistsAsync(string path);

        Task DeleteAsync(string path);

        Task DeleteDirectoryAsync(string path);

        Task<IReadOnlyList<string>> GetDirectoriesAsync(string path);

        Task<IReadOnlyList<string>> GetFilesAsync(string path);

        Task CreateDirectoryAsync(string path);
    }
}