using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.AI;
using Codinex.Core.Interfaces.Services;
using Codinex.Core.Models.AI;

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
            "ernie",
            "mimo",
            "minimax"
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

        /// <summary>
    /// Determines whether this retriever can handle the specified AI provider.
    /// </summary>
    /// <param name="provider">The AI provider to check.</param>
    /// <returns><c>true</c> if the provider can be handled by this retriever; otherwise, <c>false</c>.</returns>
    public bool CanHandle(AiProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            // OpenCode's free-model provider uses its own "GET /provider" discovery format
            // (OpenCodeFreeModelRetriever), and Gemini uses "GET /models" with a "models[].name"
            // shape (GeminiModelRetriever); neither matches the OpenAI "data[].id" shape here.
            return !string.IsNullOrWhiteSpace(provider.ModelEndPoint)
                   && !string.Equals(provider.Protocol, "opendcodefree", StringComparison.OrdinalIgnoreCase)
                   && !string.Equals(provider.Protocol, "gemini", StringComparison.OrdinalIgnoreCase);
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

            if (string.Equals(provider.Protocol, "anthropic", StringComparison.OrdinalIgnoreCase))
                model.Family = AiProviderFamily.Anthropic;

            if (string.Equals(provider.Protocol, "gemini", StringComparison.OrdinalIgnoreCase))
                model.Family = AiProviderFamily.GoogleGemini;

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