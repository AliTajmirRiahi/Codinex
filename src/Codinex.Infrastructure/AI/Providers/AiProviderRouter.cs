using System;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Storage.Managers;

namespace Codinex.Infrastructure.AI.Providers
{
    /// <summary>
    /// Runtime resolver for AI providers. It reuses the active provider instance until the active provider configuration changes.
    /// </summary>
    [AutoDiRegister(Modules.AI, RegistrationOrder.Infrastructure)]
    public class AiProviderRouter(
        ProviderManager providerManager,
        IAiProviderFactory providerFactory)
        : IAiProviderRouter,
          IDisposable
    {
        private readonly object _sync = new();
        private IAiProvider _currentProvider;
        private string _currentProviderKey;
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

                DisposeCurrentProvider();

                _currentProvider = providerFactory.Create(provider);
                _currentProviderKey = providerKey;

                return _currentProvider;
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

                DisposeCurrentProvider();
                _disposed = true;
            }
        }

        private static string BuildProviderKey(AiProvider provider)
        {
            return string.Join(
                "|",
                provider.Id ?? string.Empty,
                provider.Protocol ?? string.Empty,
                provider.BaseUrl ?? string.Empty,
                provider.ApiKey ?? string.Empty);
        }

        private void DisposeCurrentProvider()
        {
            if (_currentProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _currentProvider = null;
            _currentProviderKey = null;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AiProviderRouter));
        }
    }
}
