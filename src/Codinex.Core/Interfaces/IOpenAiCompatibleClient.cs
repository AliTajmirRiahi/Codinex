using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models;

namespace Codify.Core.Interfaces
{
    public interface IOpenAiCompatibleClient
    {
        /// <summary>
        /// Sends a GET request to an OpenAI-compatible endpoint.
        /// </summary>
        Task<string> GetAsync(
            AiProvider provider,
            string endpoint,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a POST request to an OpenAI-compatible endpoint.
        /// </summary>
        Task<string> PostAsync(
            AiProvider provider,
            string endpoint,
            object payload,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a streaming POST request to an OpenAI-compatible endpoint.
        /// Returns the raw SSE data lines.
        /// </summary>
        IAsyncEnumerable<string> StreamPostAsync(
            AiProvider provider,
            string endpoint,
            object payload,
            CancellationToken cancellationToken = default);
    }
}