using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Storage.Interfaces;
using Codinex.Storage.Models.DTO;
using Codinex.Storage.Services;

namespace Codinex.Storage.Managers
{
    [AutoDiRegister(Modules.Storage, RegistrationOrder.Foundation)]
    public class ProviderManager(IStorageService storage,
        IJsonSerializer jsonSerializer,
        IProviderModelService providerModelService,
        IProviderCapabilityChecker providerCapabilityChecker)
    {
        private readonly IJsonSerializer _jsonSerializer = jsonSerializer;

        public List<AiProvider> Providers { get; private set; } = [];

        public AiProvider ActiveProvider => Providers.FirstOrDefault(p => p.IsEnabled);

        public AiModel ActiveModel => ActiveProvider?.Models?.FirstOrDefault(p => p.IsCurrent);

        public async Task InitializeAsync()
        {
            if (await storage.ExistsAsync(StoragePaths.Providers))
            {
                Providers = await storage.LoadAsync<List<AiProvider>>(StoragePaths.Providers)
                             ?? await GetDefaultProviders();

                if (await MergeNewDefaultProvidersAsync())
                    await SaveAsync();
            }
            else
            {
                Providers = await GetDefaultProviders();
                await SaveAsync();
            }
        }

        /// <summary>
        /// Adds any provider present in the bundled providers.json but missing from the
        /// user's saved provider list (e.g. after an extension update introduced a new
        /// provider). Existing saved providers (settings, API keys, selected models) are
        /// left untouched.
        /// </summary>
        /// <returns>True if one or more new providers were added.</returns>
        private async Task<bool> MergeNewDefaultProvidersAsync()
        {
            var defaultProviders = LoadResourceCollection<AiProvider>("providers.json");

            var existingIds = new HashSet<string>(Providers.Select(p => p.Id), StringComparer.OrdinalIgnoreCase);

            var newProviders = defaultProviders
                .Where(p => !existingIds.Contains(p.Id))
                .ToList();

            if (newProviders.Count == 0)
                return false;

            await PopulateModelsAsync(newProviders);

            Providers.AddRange(newProviders);

            return true;
        }

        private async Task<List<AiProvider>> GetDefaultProviders()
        {
            var providers = LoadResourceCollection<AiProvider>("providers.json");

            await PopulateModelsAsync(providers);

            return providers;
        }

        private async Task PopulateModelsAsync(IEnumerable<AiProvider> providers)
        {
            foreach (var provider in providers)
            {
                provider.SetModels(await providerModelService.GetModelsAsync(provider));
            }
        }

        /// <summary>
        /// Loads a list of models from a JSON file located inside the VSIX Resources folder.
        /// Any exception is intentionally not caught here so it can be handled by the ExecutionPipeline.
        /// </summary>
        private List<T> LoadResourceCollection<T>(string modelFileName)
        {
            // Get the directory of the executing assembly (Codinex.dll location)
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
            await storage.SaveAsync(StoragePaths.Providers, Providers);
        }

        public async Task AddModelToProviderAsync(string providerId, AiModel newModel)
        {
            var provider = Providers.FirstOrDefault(p => p.Id == providerId);
            if (provider != null)
            {
                provider.AddModel(newModel);
                await SaveAsync();
            }
        }

        public async Task RefreshModelsAsync(string providerId)
        {
            if (string.IsNullOrWhiteSpace(providerId))
                throw new ArgumentException(@"ProviderId is required.", nameof(providerId));

            var provider = Providers.FirstOrDefault(p =>
                string.Equals(p.Id, providerId, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
                throw new InvalidOperationException($"Provider '{providerId}' was not found.");

            var refreshedModels = (await providerModelService.GetModelsAsync(provider)).ToList();

            PreserveModelRuntimeState(provider, refreshedModels);

            provider.SetModels(refreshedModels);

            await SaveAsync();
        }

        public async Task<ProviderSettingsUpdateResult> UpdateSettingsAsync(AiProviderDto selectedProvider)
        {
            if (selectedProvider == null)
                throw new ArgumentNullException(nameof(selectedProvider));

            if (string.IsNullOrWhiteSpace(selectedProvider.ProviderId))
                throw new ArgumentException(@"ProviderId is required.", nameof(selectedProvider));

            var provider = Providers.FirstOrDefault(p =>
                string.Equals(p.Id, selectedProvider.ProviderId, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
                throw new InvalidOperationException($"Provider '{selectedProvider.ProviderId}' was not found.");

            if (provider.IsLocal)
            {
                try
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                    var installedModels = (await providerModelService.GetModelsFromServerAsync(provider, timeout.Token)).ToList();

                    PreserveModelRuntimeState(provider, installedModels);

                    provider.SetModels(installedModels);
                }
                catch (Exception)
                {
                    return ProviderSettingsUpdateResult.Failed(
                        $"{provider.Name} is unavailable. Make sure its local HTTP API is running and the models endpoint responds within 1 second.",
                        false);
                }
            }

            if (selectedProvider.SelectedModels == null || selectedProvider.SelectedModels.Count == 0)
                return ProviderSettingsUpdateResult.Failed("At least one model must be selected.");

            foreach (var prov in Providers)
                prov.Disable();

            provider.SetApiKey(selectedProvider.ApiKey);
            provider.Enable();

            var previousCurrentModelId = provider.Models.FirstOrDefault(m => m.IsCurrent)?.Id;

            foreach (var model in provider.Models)
            {
                model.DeSelect();
                model.ClearCurrent();
            }

            var selectedModel = provider.Models
                .Where(m => selectedProvider.SelectedModels.Any(sm => string.Equals(m.Id, sm.Id, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (provider.IsLocal && selectedModel.Count == 0)
            {
                provider.Disable();

                return ProviderSettingsUpdateResult.Failed(
                    $"None of the selected models are installed for {provider.Name}. Refresh the model list and select an installed model.");
            }

            foreach (var model in selectedModel)
                model.Select();

            var currentModel = selectedModel.FirstOrDefault(m =>
                string.Equals(m.Id, previousCurrentModelId, StringComparison.OrdinalIgnoreCase))
                ?? selectedModel.FirstOrDefault();

            currentModel?.MarkAsCurrent();

            await providerCapabilityChecker.CheckAsync(provider, currentModel, CancellationToken.None);

            await SaveAsync();

            await InitializeAsync();

            return ProviderSettingsUpdateResult.Saved();
        }

        private static void PreserveModelRuntimeState(AiProvider provider, IReadOnlyCollection<AiModel> refreshedModels)
        {
            if (provider == null || refreshedModels == null || refreshedModels.Count == 0)
                return;

            var existingModels = provider.Models.ToDictionary(m => m.Id, StringComparer.OrdinalIgnoreCase);

            foreach (var refreshedModel in refreshedModels)
            {
                if (!existingModels.TryGetValue(refreshedModel.Id, out var existingModel))
                    continue;

                if (existingModel.IsSelected)
                    refreshedModel.Select();

                if (existingModel.IsCurrent)
                    refreshedModel.MarkAsCurrent();

                if (existingModel.CapabilitiesChecked)
                {
                    refreshedModel.UpdateCapabilities(
                        existingModel.SupportsStreaming,
                        existingModel.SupportsToolCalling,
                        existingModel.SupportsVision,
                        existingModel.SupportsReasoning);
                }
            }
        }

        public async Task SetCurrentModelAsync(AiModelSelectedDto payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            if (string.IsNullOrWhiteSpace(payload.ProviderId))
                throw new ArgumentException(@"ProviderId is required.", nameof(payload));

            if (string.IsNullOrWhiteSpace(payload.ModelId))
                throw new ArgumentException(@"ModelId is required.", nameof(payload));

            var provider = Providers.FirstOrDefault(p => p.Id == payload.ProviderId);
            if (provider == null)
                throw new InvalidOperationException($"Provider '{payload.ProviderId}' was not found.");

            var model = provider.Models.FirstOrDefault(m => m.IsCurrent == true);
            model?.ClearCurrent();

            model = provider.Models.FirstOrDefault(m => m.Id == payload.ModelId);
            if (model == null)
                throw new InvalidOperationException($"Model '{payload.ModelId}' was not found.");

            model.MarkAsCurrent();

            await providerCapabilityChecker.CheckAsync(provider, model, CancellationToken.None);

            await SaveAsync();

            await InitializeAsync();
        }
    }
}
