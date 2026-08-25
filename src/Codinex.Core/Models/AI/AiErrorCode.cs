namespace Codinex.Core.Models.AI
{
    /// <summary>
    /// Provider-agnostic categories for expected AI provider failures.
    /// </summary>
    public enum AiErrorCode
    {
        Network = 0,
        Timeout,
        AuthenticationFailed,
        InvalidApiKey,
        UnsupportedRegion,
        InsufficientCredits,
        RateLimitExceeded,
        ContextLengthExceeded,
        ModelNotFound,
        ProviderUnavailable,
        ContentFiltered,
        Cancelled,
        Unknown
    }
}
