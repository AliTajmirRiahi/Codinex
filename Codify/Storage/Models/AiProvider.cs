using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Codify.Storage.Models
{
    public class AiProvider
    {
        // Unique provider identifier (e.g. "gapgpt", "openai")
        public string Id { get; }

        // Display name of the provider
        public string Name { get; private set; }

        // Secret API key used for authentication
        public string ApiKey { get; private set; }

        // Base URL endpoint for API requests
        public string BaseUrl { get; private set; }

        // Available models for this provider
        public IReadOnlyCollection<AiModel> Models => _models.AsReadOnly();

        // Indicates whether the provider is active
        public bool IsEnabled { get; private set; }

        private readonly List<AiModel> _models = new();

        /// <summary>
        /// Constructor enforces required invariants.
        /// </summary>
        public AiProvider(string id, string name, string baseUrl) : this(id, name, "", baseUrl, false, new List<AiModel>())
        {
            
        }

        /// <summary>
        /// Constructor used by Newtonsoft during deserialization.
        /// </summary>
        [JsonConstructor]
        private AiProvider(
            string id,
            string name,
            string apiKey,
            string baseUrl,
            bool isEnabled,
            List<AiModel> models)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException(@"Provider id cannot be empty.", nameof(id));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(@"Provider name cannot be empty.", nameof(name));

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException(@"BaseUrl cannot be empty.", nameof(baseUrl));

            Id = id;
            Name = name;
            ApiKey = apiKey ?? "";
            BaseUrl = baseUrl ?? "";
            IsEnabled = isEnabled;

            _models = models ?? new List<AiModel>();
        }
        /// <summary>
        /// Updates the API key securely.
        /// </summary>
        public void SetApiKey(string apiKey)
        {
            ApiKey = apiKey ?? string.Empty;
        }

        /// <summary>
        /// Enables the provider.
        /// </summary>
        public void Enable()
        {
            IsEnabled = true;
        }

        /// <summary>
        /// Disables the provider.
        /// </summary>
        public void Disable()
        {
            IsEnabled = false;
        }

        /// <summary>
        /// Replaces the full model list.
        /// </summary>
        public void SetModels(IEnumerable<AiModel> models)
        {
            _models.Clear();

            if (models == null)
                return;

            _models.AddRange(models);
        }

        /// <summary>
        /// Adds a new model if not already present.
        /// </summary>
        public void AddModel(AiModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (_models.Any(m => m.Id == model.Id))
                return;

            _models.Add(model);
        }

        /// <summary>
        /// Removes a model by id.
        /// </summary>
        public void RemoveModel(string modelId)
        {
            var model = _models.FirstOrDefault(m => m.Id == modelId);
            if (model != null)
                _models.Remove(model);
        }

        /// <summary>
        /// Updates provider display name.
        /// </summary>
        public void Rename(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException(@"Name cannot be empty.", nameof(newName));

            Name = newName;
        }

        /// <summary>
        /// Updates base URL endpoint.
        /// </summary>
        public void SetBaseUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException(@"BaseUrl cannot be empty.", nameof(baseUrl));

            BaseUrl = baseUrl;
        }
    }

}