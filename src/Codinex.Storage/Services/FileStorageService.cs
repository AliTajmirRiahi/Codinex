using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Storage.Interfaces;
using Newtonsoft.Json;

namespace Codinex.Storage.Services
{
    /// <summary>
    /// File-based JSON storage implementation.
    /// </summary>
    [AutoDiRegister(Modules.Storage, RegistrationOrder.Foundation, ServiceLifetimeWrapper.Singleton)]
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

            // Run synchronous file I/O in a background thread.
            // File.WriteAllText does not create missing parent directories, so ensure them first.
            await Task.Run(() =>
            {
                var directory = fileSystem.Path.GetDirectoryName(path);

                if (!string.IsNullOrWhiteSpace(directory) && !fileSystem.Directory.Exists(directory))
                {
                    fileSystem.Directory.CreateDirectory(directory);
                }

                fileSystem.File.WriteAllText(path, json);
            });
        }

        public async Task<T> LoadAsync<T>(string path)
        {
            await Task.Yield();

            if (!fileSystem.File.Exists(path))
                return default;

            var json = fileSystem.File.ReadAllText(path);

            return JsonConvert.DeserializeObject<T>(json);
        }

        public Task<bool> ExistsAsync(string path)
        {
            return Task.FromResult(fileSystem.Directory.Exists(path) || fileSystem.File.Exists(path));
        }

        public async Task DeleteAsync(string path)
        {
            await Task.Run(() =>
            {
                if (fileSystem.File.Exists(path))
                {
                    fileSystem.File.Delete(path);
                }
            });
        }

        public async Task DeleteDirectoryAsync(string path)
        {
            await Task.Run(() =>
            {
                fileSystem.Directory.Delete(path, recursive: true);
            });
        }

        public async Task<IReadOnlyList<string>> GetDirectoriesAsync(string path)
        {
            return await Task.Run<IReadOnlyList<string>>(() => [.. fileSystem.Directory.GetDirectories(path)]);
        }

        public async Task<IReadOnlyList<string>> GetFilesAsync(string path)
        {
            return await Task.Run<IReadOnlyList<string>>(() => [.. fileSystem.Directory.GetFiles(path)]);
        }

        public async Task CreateDirectoryAsync(string path)
        {
            await Task.Run(() =>
            {
                fileSystem.Directory.CreateDirectory(path);
            });
        }
    }
}