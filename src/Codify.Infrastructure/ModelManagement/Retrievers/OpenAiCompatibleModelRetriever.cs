using Codify.Core.Interfaces;
using Codify.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.ModelManagement.Retrievers
{
    public class OpenAiCompatibleModelRetriever(
        IOpenAiCompatibleClient client,
        IJsonSerializer jsonSerializer) : IModelRetriever
    {

        private static readonly string[] ExcludedPrefixes =
        [
            "whisper",
            "tts",
            "dall-e",
            "gpt-image",
            "omni-moderation",
            "text-embedding",
            "embedding"
        ];

        private static readonly string[] ExcludedNames =
        [
            "sora"
        ];

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

            var items = json["data"];

            return items == null ? [] : (from item in items select item["id"]?.ToString() into id where !string.IsNullOrWhiteSpace(id) && IsSupportedModel(id) select AiModel.CreateRemote(id)).ToList();
        }

        private static bool IsSupportedModel(string modelId)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                return false;

            modelId = modelId.ToLowerInvariant();

            if (ExcludedNames.Contains(modelId))
                return false;

            return !ExcludedPrefixes.Any(modelId.StartsWith);
        }
    }
}