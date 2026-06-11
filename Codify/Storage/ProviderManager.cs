using Codify.Storage.Models;
using Codify.UI.ToolWindows;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Codify.Storage
{
    public class ProviderManager
    {
        private readonly IStorageService _storage;
        private List<AiProvider> _providers = new List<AiProvider>();

        public ProviderManager(IStorageService storage)
        {
            _storage = storage;
        }

        public List<AiProvider> AllProviders => _providers;

        public AiProvider ActiveProvider => _providers.FirstOrDefault(p => p.IsEnabled);

        public async Task InitializeAsync()
        {
            if (await _storage.ExistsAsync(StoragePaths.Providers))
            {
                _providers = await _storage.LoadAsync<List<AiProvider>>(StoragePaths.Providers)
                             ?? GetDefaultProviders();
            }
            else
            {
                _providers = GetDefaultProviders();
                await SaveAsync();
            }
        }
        private List<AiProvider> GetDefaultProviders()
        {
            var providers = LoadModelsFromResources<AiProvider>("providers.json");

            foreach (var provider in providers)
            {
                provider.Models = LoadModelsFromResources<AiModel>($"{provider.Id}_models.json");
            }

            return providers;
        }

        private List<T> LoadModelsFromResources<T>(string modelFileName)
        {
            try
            {
                // Get the directory of the executing assembly (Codify.dll location)
                string assemblyLocation = Assembly.GetExecutingAssembly().Location;
                string assemblyDir = Path.GetDirectoryName(assemblyLocation);

                // Path inside the VSIX installation folder
                string resourcePath = Path.Combine(assemblyDir, "Resources", modelFileName);

                // Logging for debugging in "Output" window
                System.Diagnostics.Debug.WriteLine($"[Codify] Looking for resource at: {resourcePath}");

                if (File.Exists(resourcePath))
                {
                    string json = File.ReadAllText(resourcePath);
                    return JsonConvert.DeserializeObject<List<T>>(json);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[Codify] Resource file NOT found!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Codify] Resource Load Error: {ex.Message}");
            }

            return new List<T>();
        }

        public async Task SaveAsync()
        {
            await _storage.SaveAsync(StoragePaths.Providers, _providers);
        }

        public async Task AddModelToProvider(string providerId, AiModel newModel)
        {
            var provider = _providers.FirstOrDefault(p => p.Id == providerId);
            if (provider != null)
            {
                provider.Models.Add(newModel);
                await SaveAsync();
            }
        }
    }
}
