using System;
using System.Linq;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Storage.Managers;

namespace Codinex.Infrastructure.AI.Providers
{
    /// <summary>
    /// Runtime resolver for AI providers. It keeps primary and preprocessor providers independent.
    /// </summary>
    [AutoDiRegister(Modules.AI, RegistrationOrder.Infrastructure)]
    public class AiProviderRouter(
        ProviderManager providerManager,
        SettingsManager settingsManager,
        IAiProviderFactory providerFactory)
        : IAiProviderRouter,
          IDisposable
    {
        private readonly object _sync = new();
        private IAiProvider _currentProvider;
        private string _currentProviderKey;
        private IAiPreprocessorProvider _currentPreprocessorProvider;
        private string _currentPreprocessorProviderKey;
        private bool _disposed;

        public IAiProvider GetCurrentProvider()
        {
            var provider = providerManager.ActiveProvider
                           ?? throw new InvalidOperationException("Active Provider not found.");

            var providerKey = BuildProviderKey(provider);

            lock (_sync)
            {
                ThrowIfDisposed();

                if (_currentProvider != null &&
                    string.Equals(_currentProviderKey, providerKey, StringComparison.Ordinal))
                {
                    return _currentProvider;
                }

                DisposeProvider(ref _currentProvider, ref _currentProviderKey);

                _currentProvider = providerFactory.Create(provider);
                _currentProviderKey = providerKey;

                return _currentProvider;
            }
        }

        public IAiPreprocessorProvider GetCurrentPreprocessorProvider()
        {
            var primaryProvider = providerManager.ActiveProvider
                                  ?? throw new InvalidOperationException("Active Provider not found.");

            var preprocessorProvider = GetConfiguredPreprocessorProvider(primaryProvider);
            var preprocessorModelId = settingsManager.Settings.PreprocessorAiModelId;

            if (preprocessorProvider == null)
            {
                return null;
            }

            var providerKey = BuildProviderKey(preprocessorProvider, preprocessorModelId);

            lock (_sync)
            {
                ThrowIfDisposed();

                if (_currentPreprocessorProvider != null &&
                    string.Equals(_currentPreprocessorProviderKey, providerKey, StringComparison.Ordinal))
                {
                    return _currentPreprocessorProvider;
                }

                DisposeProvider(ref _currentPreprocessorProvider, ref _currentPreprocessorProviderKey);

                var provider = providerFactory.Create(preprocessorProvider);

                if (provider is IAiPreprocessorProvider preprocessor)
                {
                    _currentPreprocessorProvider = preprocessor;
                    _currentPreprocessorProviderKey = providerKey;

                    return _currentPreprocessorProvider;
                }

                if (provider is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                return null;
            }
        }

        public IAiProvider GetProvider(AgentContext context)
        {
            // Future multi-agent routing can resolve a provider from AgentContext here.
            // Until agent-specific provider mappings exist, the current provider remains the default route.
            return GetCurrentProvider();
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed)
                    return;

                DisposeProvider(ref _currentProvider, ref _currentProviderKey);
                DisposeProvider(ref _currentPreprocessorProvider, ref _currentPreprocessorProviderKey);
                _disposed = true;
            }
        }

        private AiProvider GetConfiguredPreprocessorProvider(AiProvider primaryProvider)
        {
            if (primaryProvider == null || primaryProvider.IsLocal)
            {
                return null;
            }

            var settings = settingsManager.Settings;

            if (settings?.EnablePreprocessorAi != true ||
                string.IsNullOrWhiteSpace(settings.PreprocessorAiProviderId) ||
                string.IsNullOrWhiteSpace(settings.PreprocessorAiModelId))
            {
                return null;
            }

            return providerManager.Providers
                .FirstOrDefault(x =>
                    x?.IsLocal == true &&
                    string.Equals(x.Id, settings.PreprocessorAiProviderId, StringComparison.OrdinalIgnoreCase) &&
                    x.Models?.Any(m => string.Equals(m.Id, settings.PreprocessorAiModelId, StringComparison.OrdinalIgnoreCase)) == true);
        }

        private static string BuildProviderKey(AiProvider provider)
        {
            return BuildProviderKey(provider, GetCurrentModelId(provider));
        }

        private static string BuildProviderKey(AiProvider provider, string modelId)
        {
            return string.Join(
                "|",
                provider.Id ?? string.Empty,
                provider.Protocol ?? string.Empty,
                provider.BaseUrl ?? string.Empty,
                provider.ApiKey ?? string.Empty,
                modelId ?? string.Empty);
        }

        private static string GetCurrentModelId(AiProvider provider)
        {
            return provider?.Models?.FirstOrDefault(x => x.IsCurrent)?.Id
                   ?? provider?.Models?.FirstOrDefault(x => x.IsSelected)?.Id
                   ?? string.Empty;
        }

        private static void DisposeProvider<TProvider>(
            ref TProvider provider,
            ref string providerKey)
            where TProvider : class
        {
            if (provider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            provider = null;
            providerKey = null;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AiProviderRouter));
        }
    }
}
