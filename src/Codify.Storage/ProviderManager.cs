using Codify.Storage.Models;
using Codify.Storage.Models.DTO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Codify.Core.Interfaces;
using Codify.Core.Models;

namespace Codify.Storage
{
    public class ProviderManager(IStorageService storage, IJsonSerializer jsonSerializer)
    {
        private readonly IJsonSerializer _jsonSerializer = jsonSerializer;
        private List<AiProvider> _providers = new List<AiProvider>();

        public List<AiProvider> AllProviders => _providers;

        public AiProvider ActiveProvider => _providers.FirstOrDefault(p => p.IsEnabled);

        public AiModel ActiveModel => ActiveProvider.Models.FirstOrDefault(p => p.IsCurrent);

        public async Task InitializeAsync()
        {
            if (await storage.ExistsAsync(StoragePaths.Providers))
            {
                _providers = await storage.LoadAsync<List<AiProvider>>(StoragePaths.Providers)
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
                provider.SetModels(LoadModelsFromResources<AiModel>($"{provider.Id}_models.json"));
            }

            return providers;
        }

        /// <summary>
        /// Loads a list of models from a JSON file located inside the VSIX Resources folder.
        /// Any exception is intentionally not caught here so it can be handled by the ExecutionPipeline.
        /// </summary>
        private List<T> LoadModelsFromResources<T>(string modelFileName)
        {
            // Get the directory of the executing assembly (Codify.dll location)
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDir = Path.GetDirectoryName(assemblyLocation)!;

            // Path inside the VSIX installation folder
            var resourcePath = Path.Combine(assemblyDir, "Resources", modelFileName);

            if (!File.Exists(resourcePath))
                throw new FileNotFoundException(
                    $"Resource file not found: {modelFileName}",
                    resourcePath);

            var json = File.ReadAllText(resourcePath);

            var models = JsonConvert.DeserializeObject<List<T>>(json);

            if (models == null)
                throw new InvalidOperationException(
                    $"Failed to deserialize models from {modelFileName}");

            return models;
        }

        public async Task SaveAsync()
        {
            await storage.SaveAsync(StoragePaths.Providers, _providers);
        }

        public async Task AddModelToProviderAsync(string providerId, AiModel newModel)
        {
            var provider = _providers.FirstOrDefault(p => p.Id == providerId);
            if (provider != null)
            {
                provider.AddModel(newModel);
                await SaveAsync();
            }
        }

        public async Task UpdateSettingsAsync(AiProviderDto selectedProvider)
        {
            if (selectedProvider == null)
                throw new ArgumentNullException(nameof(selectedProvider));

            if (string.IsNullOrWhiteSpace(selectedProvider.ProviderId))
                throw new ArgumentException(@"ProviderId is required.", nameof(selectedProvider));

            var provider = _providers.FirstOrDefault(p =>
                string.Equals(p.Id, selectedProvider.ProviderId, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
                throw new InvalidOperationException($"Provider '{selectedProvider.ProviderId}' was not found.");

            if (selectedProvider.SelectedModels.Count == 0)
                throw new ArgumentException(@"At least one model must be selected.", nameof(selectedProvider));

            foreach (var prov in _providers)
                prov.Disable();

            provider.SetApiKey(selectedProvider.ApiKey);
            provider.Enable();

            foreach (var model in provider.Models)
                model.DeSelect();

            var selectedModel = provider.Models.Where(m => selectedProvider.SelectedModels.Any(sm => string.Equals(m.Id, sm.Id, StringComparison.OrdinalIgnoreCase)));

            foreach (var model in selectedModel)
                model.Select();

            var currentModel = provider.Models.FirstOrDefault(m => m.IsCurrent == true);
            if (currentModel == null)
            {
                var firstSelectedModel = provider.Models.FirstOrDefault(m => m.IsSelected);
                firstSelectedModel?.MarkAsCurrent();
            }

            await SaveAsync();

            await InitializeAsync();
        }

        public async Task SetCurrentModelAsync(AiModelSelectedDto payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            if (string.IsNullOrWhiteSpace(payload.ProviderId))
                throw new ArgumentException(@"ProviderId is required.", nameof(payload));

            if (string.IsNullOrWhiteSpace(payload.ModelId))
                throw new ArgumentException(@"ModelId is required.", nameof(payload));

            var provider = _providers.FirstOrDefault(p => p.Id == payload.ProviderId);
            if (provider == null)
                throw new InvalidOperationException($"Provider '{payload.ProviderId}' was not found.");

            var model = provider.Models.FirstOrDefault(m => m.IsCurrent == true);
            model?.ClearCurrent();

            model = provider.Models.FirstOrDefault(m => m.Id == payload.ModelId);
            if (model == null)
                throw new InvalidOperationException($"Model '{payload.ModelId}' was not found.");

            model.MarkAsCurrent();

            await SaveAsync();

            await InitializeAsync();
        }
    }
}
