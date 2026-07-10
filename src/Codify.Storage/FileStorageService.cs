using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace Codify.Storage
{
    /// <summary>
    /// File-based JSON storage implementation.
    /// </summary>
    public class FileStorageService : IStorageService
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
            await Task.Run(() => File.WriteAllText(path, json));
        }

        public async Task<T> LoadAsync<T>(string path)
        {
            if (!File.Exists(path))
                return default;

            string json;
            using (var reader = new StreamReader(path))
            {
                json = await reader.ReadToEndAsync();
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

        public Task<bool> ExistsAsync(string path)
        {
            return Task.FromResult(File.Exists(path));
        }

        public Task DeleteAsync(string path)
        {
            if (File.Exists(path))
                File.Delete(path);

            return Task.CompletedTask;
        }
    }
}