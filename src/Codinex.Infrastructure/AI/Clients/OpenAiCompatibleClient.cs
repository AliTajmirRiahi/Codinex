using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Infrastructure.CustomeExceptions;

namespace Codinex.Infrastructure.AI.Clients
{
    [AutoDiRegister(Modules.AI, RegistrationOrder.Features)]
    public class OpenAiCompatibleClient(
        IHttpService httpService,
        IJsonSerializer jsonSerializer,
        IWorkspaceFileService workspaceFileService)
        : IOpenAiCompatibleClient
    {

        public async Task<string> GetAsync(
            AiProvider provider,
            string endpoint,
            CancellationToken cancellationToken = default)
        {
            using var request = CreateRequest(
                HttpMethod.Get,
                provider,
                endpoint);

            using var response = await httpService.SendAsync(
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode) return await response.Content.ReadAsStringAsync();

            var body = await response.Content.ReadAsStringAsync();

            throw new OpenAiCompatibleException(
                response.StatusCode,
                body,
                response.Headers.RetryAfter?.Delta);

        }

        public async Task<string> PostAsync(
            AiProvider provider,
            string endpoint,
            object payload,
            CancellationToken cancellationToken = default)
        {
            using var request = CreateRequest(
                HttpMethod.Post,
                provider,
                endpoint);

            request.Content = new StringContent(
                jsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await httpService.SendAsync(
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode) return await response.Content.ReadAsStringAsync();

            var body = await response.Content.ReadAsStringAsync();

            throw new OpenAiCompatibleException(
                response.StatusCode,
                body,
                response.Headers.RetryAfter?.Delta);

        }

        public async IAsyncEnumerable<string> StreamPostAsync(
            AiProvider provider,
            string endpoint,
            object payload,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var request = CreateRequest(
                HttpMethod.Post,
                provider,
                endpoint);

            request.Content = new StringContent(
                jsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await httpService.SendAsync(
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();

                throw new OpenAiCompatibleException(
                    response.StatusCode,
                    body,
                    response.Headers.RetryAfter?.Delta);
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!cancellationToken.IsCancellationRequested)
            {
                var readTask = reader.ReadLineAsync();

                var completed = await Task.WhenAny(
                    readTask,
                    Task.Delay(TimeSpan.FromSeconds(300), cancellationToken));

                if (completed != readTask)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    throw new TimeoutException(
                        "No SSE event received for 300 seconds.");
                }

                var line = await readTask;

                if (line == null)
                    yield break;

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

                request.Headers.ConnectionClose = true;
                request.Headers.Connection.Add("close");
            }

            return request;
        }
    }
}