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
    /// <summary>
    /// Discovers models for the Gemini "generativelanguage" REST API. Its "GET /models" response
    /// wraps entries as { "models": [ { "name": "models/gemini-2.0-flash",
    /// "supportedGenerationMethods": [...] } ] }, so it needs its own parser rather than the
    /// OpenAI "/models" "data[].id" shape handled by <see cref="OpenAiCompatibleModelRetriever"/>.
    /// </summary>
    [AutoDiRegister(Modules.ModelManagement, RegistrationOrder.Infrastructure)]
    public class GeminiModelRetriever(
        IProviderClient client,
        IJsonSerializer jsonSerializer) : IModelRetriever
    {
        public bool CanHandle(AiProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            return string.Equals(provider.Protocol, "gemini", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<IReadOnlyList<AiModel>> GetModelsAsync(
            AiProvider provider,
            CancellationToken cancellationToken = default)
        {
            var endpoint = string.IsNullOrWhiteSpace(provider.ModelEndPoint)
                ? "/models"
                : provider.ModelEndPoint;

            var response = await client.GetAsync(
                provider,
                endpoint,
                cancellationToken);

            var json = jsonSerializer.Parse(response);
            var items = json["models"];

            if (items == null)
                return [];

            return (from item in items
                    where SupportsGenerateContent(item)
                    let id = NormalizeModelId(item["name"]?.ToString())
                    where !string.IsNullOrWhiteSpace(id)
                    select CreateModel(id)).ToList();
        }

        private static bool SupportsGenerateContent(Newtonsoft.Json.Linq.JToken item)
        {
            var methods = item["supportedGenerationMethods"];

            if (methods == null)
                return true;

            return methods.Any(x => string.Equals(
                x.ToString(),
                "generateContent",
                StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeModelId(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            name = name.Trim();

            return name.StartsWith("models/", StringComparison.OrdinalIgnoreCase)
                ? name.Substring("models/".Length)
                : name;
        }

        private static AiModel CreateModel(string id)
        {
            var model = AiModel.CreateRemote(id);

            model.Family = AiProviderFamily.GoogleGemini;

            return model;
        }
    }
}
