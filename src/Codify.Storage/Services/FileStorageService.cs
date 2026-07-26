using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Codify.Core.Interfaces;
using Codify.Storage.Interfaces;
using Newtonsoft.Json;

namespace Codify.Storage.Services
{
    /// <summary>
    /// File-based JSON storage implementation.
    /// </summary>
    public class FileStorageService(IFileSystem fileSystem) : IStorageService
    {
        private readonly JsonSerializerSettings _settings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        public async Task SaveAsync<T>(string path, T data)
        {
            var json = JsonConvert.SerializeObject(data, _settings);

            // Run synchronous File.WriteAllText in a background thread
            await Task.Run(() => fileSystem.WriteAllText(path, json));
        }

        public async Task<T> LoadAsync<T>(string path)
        {
            await Task.Yield();

            if (!fileSystem.Exists(path))
                return default;

            var json = fileSystem.ReadAllText(path);

            return JsonConvert.DeserializeObject<T>(json);
        }

        public Task<bool> ExistsAsync(string path)
        {
            return Task.FromResult(fileSystem.Exists(path));
        }

        public async Task DeleteAsync(string path)
        {
            await Task.Run(() =>
            {
                if (fileSystem.FileExists(path))
                {
                    fileSystem.DeleteFile(path);
                }
            });
        }

        public async Task DeleteDirectoryAsync(string path)
        {
            await Task.Run(() =>
            {
                fileSystem.DeleteDirectory(path, recursive: true);
            });
        }

        public async Task<IReadOnlyList<string>> GetDirectoriesAsync(string path)
        {
            return await Task.Run<IReadOnlyList<string>>(() => fileSystem
                .GetDirectories(path)
                .ToList());
        }

        public async Task<IReadOnlyList<string>> GetFilesAsync(string path)
        {
            return await Task.Run<IReadOnlyList<string>>(() => fileSystem
                .GetFiles(path)
                .ToList());
        }

        public async Task CreateDirectoryAsync(string path)
        {
            await Task.Run(() =>
            {
                fileSystem.CreateDirectory(path);
            });
        }
    }
}