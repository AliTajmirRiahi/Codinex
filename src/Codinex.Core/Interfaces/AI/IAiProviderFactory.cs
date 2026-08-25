using Codinex.Core.Models.AI;

namespace Codinex.Core.Interfaces.AI
{
    /// <summary>
    /// Creates AI provider implementations. Implementations must not cache provider instances.
    /// </summary>
    public interface IAiProviderFactory
    {
        /// <summary>
        /// Creates a fresh provider instance for the specified provider configuration.
        /// </summary>
        IAiProvider Create(AiProvider provider);
    }
}
