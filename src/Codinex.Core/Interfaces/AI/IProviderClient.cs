using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces.AI
{
    public interface IProviderClient
    {
        /// <summary>
        /// Sends a GET request to a provider endpoint.
        /// </summary>
        Task<string> GetAsync(
            AiProvider provider,
            string endpoint,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a POST request to a provider endpoint.
        /// </summary>
        Task<string> PostAsync(
            AiProvider provider,
            string endpoint,
            object payload,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a streaming POST request to a provider endpoint.
        /// Returns raw provider data lines.
        /// </summary>
        IAsyncEnumerable<string> StreamPostAsync(
            AiProvider provider,
            string endpoint,
            object payload,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Opens a streaming GET request (Server-Sent Events) to a provider endpoint.
        /// Returns raw provider data lines.
        /// </summary>
        IAsyncEnumerable<string> StreamGetAsync(
            AiProvider provider,
            string endpoint,
            CancellationToken cancellationToken = default);
    }
}