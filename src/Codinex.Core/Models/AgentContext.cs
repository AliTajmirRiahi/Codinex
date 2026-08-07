namespace Codinex.Core.Models
{
    /// <summary>
    /// Describes the agent requesting an AI provider. This keeps provider routing extensible for future multi-agent scenarios.
    /// </summary>
    public sealed class AgentContext
    {
        public string ProviderId { get; set; }
    }
}
