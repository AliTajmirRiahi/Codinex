using Codify.Core.Models;
using Codify.Storage.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Core.Abstractions;

public class AiProviderInfo
{
    public AiProviderInfo(AiProviderFamily family, IAiProvider aiProvider)
    {
        Family = family;
        AiProvider = aiProvider;
    }

    public AiProviderFamily Family { get; set; }

    public IAiProvider AiProvider { get; set; }
}

public interface IAiRouterProvider
{
    /// <summary>
    /// Gets the collection of AI providers available for use.
    /// </summary>
    /// <remarks>This property provides access to a set of implementations of the IAiProvider interface,
    /// allowing users to interact with various AI services. The collection may be empty if no providers are
    /// registered.</remarks>
    IEnumerable<AiProviderInfo> Providers { get; }

    /// <summary>
    /// Sends a prompt along with multiple attachments (code, files, images) to the AI.
    /// </summary>
    Task<string> SendAsync(
        IReadOnlyList<ChatMessage> prompt,
        CancellationToken ct = default);
}