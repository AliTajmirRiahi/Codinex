using System;

namespace Codinex.Core.Models.AI
{
    /// <summary>
    /// Thrown by <see cref="Codinex.Core.Interfaces.AI.IProviderCapabilityChecker"/> when the
    /// provider itself is unusable (invalid API key, no credits, rate limited, provider down,
    /// unsupported region) rather than a single capability being unsupported.
    /// Carries a user-safe <see cref="AiError"/> so callers can surface a meaningful message
    /// instead of silently marking every capability as <c>Unknown</c>.
    /// </summary>
    public sealed class ProviderCapabilityException : Exception
    {
        public ProviderCapabilityException(AiError error)
            : base(error?.Message ?? "The AI provider returned an error.")
        {
            Error = error ?? new AiError(
                AiErrorCode.Unknown,
                "The AI provider returned an error.",
                false);
        }

        /// <summary>
        /// Provider-agnostic, user-safe details for the failure that made the provider unusable.
        /// </summary>
        public AiError Error { get; }
    }
}
