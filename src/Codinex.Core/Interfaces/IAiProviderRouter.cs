using Codinex.Core.Conversation;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces
{
    /// <summary>
    /// Resolves the AI provider that should be used at runtime.
    /// </summary>
    public interface IAiProviderRouter
    {
        /// <summary>
        /// Gets the provider for the currently active provider configuration.
        /// </summary>
        IAiProvider GetCurrentProvider();

        /// <summary>
        /// Gets the configured preprocessor provider for the current primary provider, if one is available.
        /// </summary>
        IAiPreprocessorProvider GetCurrentPreprocessorProvider();

        /// <summary>
        /// Gets the model identity currently used by the specified provider role.
        /// </summary>
        (string ProviderId, string ProviderName, string ModelId, string ModelName) GetCurrentModelIdentity(
            ConversationProviderRole providerRole);

        /// <summary>
        /// Gets the provider for an agent context. The current implementation may fall back to the active provider,
        /// while preserving the contract needed for future multi-agent provider routing.
        /// </summary>
        IAiProvider GetProvider(AgentContext context);
    }
}
