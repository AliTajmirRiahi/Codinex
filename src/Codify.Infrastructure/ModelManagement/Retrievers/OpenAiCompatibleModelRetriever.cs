using Codify.Core.Interfaces;
using Codify.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.ModelManagement.Retrievers
{
    public class OpenAiCompatibleModelRetriever(
        IOpenAiCompatibleClient client,
        IJsonSerializer jsonSerializer) : IModelRetriever
    {
        public bool CanHandle(AiProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            return provider.Protocol.Equals(
                "openai",
                StringComparison.OrdinalIgnoreCase);
        }

        public async Task<IReadOnlyList<AiModel>> GetModelsAsync(
            AiProvider provider,
            CancellationToken cancellationToken = default)
        {
            var response = await client.GetAsync(
                provider,
                "/models",
                cancellationToken);

            var json = jsonSerializer.Parse(response);

            var models = new List<AiModel>();

            var items = json["data"];

            if (items == null)
                return new List<AiModel>();

            foreach (var item in items)
            {
                var id = item["id"]?.ToString();

                if (string.IsNullOrWhiteSpace(id))
                    continue;

                models.Add(AiModel.CreateRemote(id));
            }

            return models;
        }
    }
}