using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;

namespace Codinex.Infrastructure.ModelManagement.Retrievers
{
    [AutoDiRegister(Modules.ModelManagement, RegistrationOrder.Infrastructure)]
    public class OpenAiCompatibleModelRetriever(
        IProviderClient client,
        IJsonSerializer jsonSerializer) : IModelRetriever
    {

        private static readonly string[] SupportedModelKeywords =
        [
            "chatgpt",
            "gpt",
            "claude",
            "gemini",
            "llama",
            "mistral",
            "mixtral",
            "codestral",
            "codellama",
            "code",
            "coder",
            "starcoder",
            "deepseek",
            "qwen",
            "yi",
            "phi",
            "grok",
            "command",
            "sonar",
            "nemotron",
            "gemma",
            "glm",
            "kimi",
            "wizard",
            "devstral",
            "hermes",
            "ernie"
        ];

        private static readonly string[] SupportedModelPrefixes =
        [
            "o1",
            "o3",
            "o4"
        ];

        private static readonly string[] SupportedInstructionKeywords =
        [
            "chat",
            "instruct",
            "reasoning"
        ];

        private static readonly string[] ExcludedKeywords =
        [
            "audio",
            "babbage",
            "curie",
            "dall-e",
            "davinci",
            "edit",
            "embedding",
            "image",
            "moderation",
            "realtime",
            "sora",
            "speech",
            "tts",
            "transcribe",
            "translate",
            "whisper"
        ];

        public bool CanHandle(AiProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            return !string.IsNullOrWhiteSpace(provider.ModelEndPoint);
        }

        public async Task<IReadOnlyList<AiModel>> GetModelsAsync(
            AiProvider provider,
            CancellationToken cancellationToken = default)
        {
            var response = await client.GetAsync(
                provider,
                provider.ModelEndPoint,
                cancellationToken);

            var json = jsonSerializer.Parse(response);
            var items = json["data"] ?? json["models"];

            return items == null
                ? []
                : (from item in items
                   let id = item["id"]?.ToString()
                            ?? item["name"]?.ToString()
                            ?? item["model"]?.ToString()
                   where !string.IsNullOrWhiteSpace(id) && (provider.IsLocal || IsSupportedModel(id))
                   select CreateModel(provider, id)).ToList();
        }

        private static AiModel CreateModel(AiProvider provider, string id)
        {
            var model = AiModel.CreateRemote(id);

            if (string.Equals(provider.Protocol, "ollama", StringComparison.OrdinalIgnoreCase))
                model.Family = AiProviderFamily.Ollama;

            return model;
        }

        private static bool IsSupportedModel(string modelId)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                return false;

            modelId = modelId.Trim().ToLowerInvariant();

            if (ExcludedKeywords.Any(modelId.Contains))
                return false;

            if (SupportedModelPrefixes.Any(prefix =>
                    modelId.Equals(prefix, StringComparison.OrdinalIgnoreCase)
                    || modelId.StartsWith(prefix + "-", StringComparison.OrdinalIgnoreCase)
                    || modelId.Contains("/" + prefix + "-")))
                return true;

            if (SupportedModelKeywords.Any(modelId.Contains))
                return true;

            return SupportedInstructionKeywords.Any(modelId.Contains);
        }
    }
}