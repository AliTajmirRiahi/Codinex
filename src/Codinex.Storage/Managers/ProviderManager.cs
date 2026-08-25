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
using Codinex.Core.Interfaces.AI;
using Codinex.Core.Interfaces.Services;
using Codinex.Core.Models.AI;
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

        // ~100KB SVG file, inflated by base64 encoding plus the "data:image/svg+xml;base64," prefix.
        private const int MaxIconDataUriLength = 140_000;

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
        /// provider), and updates default metadata for existing saved providers. User
        /// specific values (API keys, enabled state, selected models) are left untouched.
        /// </summary>
        /// <returns>True if one or more providers were added or updated.</returns>
        private async Task<bool> MergeNewDefaultProvidersAsync()
        {
            var defaultProviders = LoadResourceCollection<AiProvider>("providers.json");
            var existingProviders = Providers.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
            var hasChanges = false;
            var newProviders = new List<AiProvider>();

            foreach (var defaultProvider in defaultProviders)
            {
                if (existingProviders.TryGetValue(defaultProvider.Id, out var existingProvider))
                {
                    hasChanges |= existingProvider.ApplyDefaultMetadata(defaultProvider);
                    continue;
                }

                newProviders.Add(defaultProvider);
            }

            if (newProviders.Count > 0)
            {
                await PopulateModelsAsync(newProviders);
                Providers.AddRange(newProviders);
                hasChanges = true;
            }

            return hasChanges;
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

        /// <summary>
        /// Validates a user-supplied provider definition by attempting to fetch its model list,
        /// and only persists it as a new, enabled provider (with its first model selected) if
        /// that call succeeds.
        /// </summary>
        public async Task<ProviderSettingsUpdateResult> AddCustomProviderAsync(AddCustomProviderDto dto)
        {
            var validationError = ValidateCustomProviderDto(dto);
            if (validationError != null)
                return validationError;

            var id = GenerateUniqueProviderId(dto.Name);
            var provider = BuildProviderFromDto(id, dto);

            var models = await providerModelService.GetModelsFromServerAsync(provider, CancellationToken.None);

            if (models == null || models.Count == 0)
                return ProviderSettingsUpdateResult.Failed(
                    $"Could not retrieve any models from {provider.Name}. Check the Base URL, Model Endpoint and API key.");

            provider.SetModels(models);

            foreach (var prov in Providers)
                prov.Disable();

            provider.Enable();

            var firstModel = provider.Models.FirstOrDefault();
            firstModel?.Select();
            firstModel?.MarkAsCurrent();

            Providers.Add(provider);

            await providerCapabilityChecker.CheckAsync(provider, firstModel, CancellationToken.None);

            await SaveAsync();

            return ProviderSettingsUpdateResult.Saved($"{provider.Name} added successfully.");
        }

        /// <summary>
        /// Re-validates an existing user-added provider's (possibly changed) definition against
        /// its live models endpoint, and only replaces it in place if that call succeeds.
        /// Bundled providers can't be edited this way.
        /// </summary>
        public async Task<ProviderSettingsUpdateResult> UpdateCustomProviderAsync(string providerId, AddCustomProviderDto dto)
        {
            if (string.IsNullOrWhiteSpace(providerId))
                throw new ArgumentException(@"ProviderId is required.", nameof(providerId));

            var index = Providers.FindIndex(p => string.Equals(p.Id, providerId, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
                return ProviderSettingsUpdateResult.Failed("Provider was not found.");

            var existing = Providers[index];
            if (!existing.AddByUser)
                return ProviderSettingsUpdateResult.Failed("Only custom providers can be edited.");

            var validationError = ValidateCustomProviderDto(dto);
            if (validationError != null)
                return validationError;

            var updatedProvider = BuildProviderFromDto(existing.Id, dto);

            var models = await providerModelService.GetModelsFromServerAsync(updatedProvider, CancellationToken.None);

            if (models == null || models.Count == 0)
                return ProviderSettingsUpdateResult.Failed(
                    $"Could not retrieve any models from {updatedProvider.Name}. Check the Base URL, Model Endpoint and API key.");

            PreserveModelRuntimeState(existing, models);
            updatedProvider.SetModels(models);

            if (existing.IsEnabled)
                updatedProvider.Enable();

            Providers[index] = updatedProvider;

            var currentModel = updatedProvider.Models.FirstOrDefault(m => m.IsCurrent);

            await providerCapabilityChecker.CheckAsync(updatedProvider, currentModel, CancellationToken.None);

            await SaveAsync();

            return ProviderSettingsUpdateResult.Saved($"{updatedProvider.Name} updated successfully.");
        }

        /// <summary>
        /// Validates the fields shared by adding and editing a custom provider.
        /// Returns null when the DTO is valid, or a failure result describing the first problem found.
        /// </summary>
        private static ProviderSettingsUpdateResult ValidateCustomProviderDto(AddCustomProviderDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.Name))
                return ProviderSettingsUpdateResult.Failed("Provider name is required.");

            if (string.IsNullOrWhiteSpace(dto.Protocol))
                return ProviderSettingsUpdateResult.Failed("Protocol is required.");

            if (string.IsNullOrWhiteSpace(dto.BaseUrl))
                return ProviderSettingsUpdateResult.Failed("Base URL is required.");

            if (dto.NeedApiKey && string.IsNullOrWhiteSpace(dto.ApiKey))
                return ProviderSettingsUpdateResult.Failed("API key is required.");

            var icon = (dto.Icon ?? "").Trim();
            if (!string.IsNullOrEmpty(icon))
            {
                if (!icon.StartsWith("data:image/svg+xml", StringComparison.OrdinalIgnoreCase))
                    return ProviderSettingsUpdateResult.Failed("Logo must be an embedded SVG image.");

                if (icon.Length > MaxIconDataUriLength)
                    return ProviderSettingsUpdateResult.Failed("Logo file is too large.");
            }

            var iconColor = (dto.IconColor ?? "").Trim();
            if (!string.IsNullOrEmpty(iconColor) && !IsHexColor(iconColor))
                return ProviderSettingsUpdateResult.Failed("Icon color must be a hex color (e.g. #000000).");

            return null;
        }

        private static bool IsHexColor(string value)
        {
            if (value.Length != 7 || value[0] != '#')
                return false;

            for (var i = 1; i < value.Length; i++)
            {
                var c = value[i];
                var isHexDigit = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!isHexDigit)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Builds a user-defined <see cref="AiProvider"/> from a validated DTO, normalizing the
        /// Base URL / ModelEndPoint pair. Does not touch <see cref="Providers"/>.
        /// </summary>
        private static AiProvider BuildProviderFromDto(string id, AddCustomProviderDto dto)
        {
            var baseUrl = dto.BaseUrl.Trim().TrimEnd('/');
            var modelEndPoint = "/" + (dto.ModelEndPoint ?? "").Trim().Trim('/');
            if (modelEndPoint == "/")
                modelEndPoint = "/models";

            var protocol = dto.Protocol.Trim();
            var isLocal = IsLocalProtocol(protocol);
            var icon = (dto.Icon ?? "").Trim();
            var iconColor = (dto.IconColor ?? "").Trim();

            return new AiProvider(
                id,
                dto.Name.Trim(),
                protocol,
                baseUrl,
                dto.NeedApiKey,
                isLocal,
                modelEndPoint,
                dto.ApiKey ?? "",
                icon,
                string.IsNullOrEmpty(iconColor) ? "#000000" : iconColor);
        }

        /// <summary>
        /// Determines whether a provider is local purely from its protocol, mirroring the
        /// bundled providers.json (only Ollama is flagged as a local HTTP API today).
        /// </summary>
        private static bool IsLocalProtocol(string protocol)
        {
            return string.Equals(protocol, "ollama", StringComparison.OrdinalIgnoreCase);
        }

        private string GenerateUniqueProviderId(string name)
        {
            var slug = new string(name.Trim().ToLowerInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '-')
                .ToArray());

            while (slug.Contains("--"))
                slug = slug.Replace("--", "-");

            slug = slug.Trim('-');

            if (string.IsNullOrWhiteSpace(slug))
                slug = "custom-provider";

            var id = slug;
            var suffix = 1;

            while (Providers.Any(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase)))
                id = $"{slug}-{suffix++}";

            return id;
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
