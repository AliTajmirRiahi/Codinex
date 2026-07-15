using Codify.Core.Interfaces;
using Codify.Core.Models;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.AI.Clients
{
    public class OpenAiCompatibleClient(
        IHttpClientFactory httpClientFactory,
        IJsonSerializer jsonSerializer)
        : IOpenAiCompatibleClient
    {

        public async Task<string> GetAsync(
            AiProvider provider,
            string endpoint,
            CancellationToken cancellationToken = default)
        {
            var HttpClient = httpClientFactory.CreateClient();

            using var request = CreateRequest(
                HttpMethod.Get,
                provider,
                endpoint);

            using var response = await HttpClient.SendAsync(
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> PostAsync(
            AiProvider provider,
            string endpoint,
            object payload,
            CancellationToken cancellationToken = default)
        {
            var HttpClient = httpClientFactory.CreateClient();

            using var request = CreateRequest(
                HttpMethod.Post,
                provider,
                endpoint);

            request.Content = new StringContent(
                jsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await HttpClient.SendAsync(
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async IAsyncEnumerable<string> StreamPostAsync(
            AiProvider provider,
            string endpoint,
            object payload,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var HttpClient = httpClientFactory.CreateClient();

            using var request = CreateRequest(
                HttpMethod.Post,
                provider,
                endpoint);

            request.Content = new StringContent(
                jsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await HttpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream &&
                   !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (!line.StartsWith("data:"))
                    continue;

                yield return line.Substring(5).Trim();
            }
        }

        /// <summary>
        /// Creates an HTTP request for an OpenAI-compatible endpoint.
        /// </summary>
        private static HttpRequestMessage CreateRequest(
            HttpMethod method,
            AiProvider provider,
            string endpoint)
        {
            var baseUrl = provider.BaseUrl.TrimEnd('/');

            var request = new HttpRequestMessage(
                method,
                $"{baseUrl}/{endpoint.TrimStart('/')}");

            if (!string.IsNullOrWhiteSpace(provider.ApiKey))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        provider.ApiKey);
            }

            return request;
        }
    }
}