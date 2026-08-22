using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Codinex.Core.Models
{
    public class AiProvider
    {
        // Unique provider identifier (e.g. "gapgpt", "openai")
        public string Id { get; }

        // Display name of the provider
        public string Name { get; private set; }

        // Icon name used by the UI to render the provider logo.
        public string Icon { get; private set; }

        // Icon color used by the UI to render the provider logo.
        public string IconColor { get; private set; }

        // Secret API key used for authentication
        public string ApiKey { get; private set; }

        // Base URL endpoint for API requests
        public string BaseUrl { get; private set; }

        public string Protocol { get; private set; }

        // Indicates whether this provider requires an API key for remote model retrieval.
        public bool NeedApiKey { get; private set; }

        // Indicates whether this provider runs locally and must be checked via its HTTP API before saving settings.
        public bool IsLocal { get; private set; }

        // Endpoint used to retrieve available models from this provider.
        public string ModelEndPoint { get; private set; }

        // Indicates whether this provider is a router that delegates to other providers
        public bool IsRouter { get; set; }

        // Indicates whether the provider is active
        public bool IsEnabled { get; private set; }

        // Available models for this provider
        public IReadOnlyCollection<AiModel> Models => _models.AsReadOnly();

        private readonly List<AiModel> _models;

        /// <summary>
        /// Constructor enforces required invariants.
        /// </summary>
        public AiProvider(string id, string name, string protocol, string baseUrl, string icon = "", string iconColor = "#000000") : this(id, name, protocol, icon, iconColor, "", baseUrl, false, true, false, "/models", new List<AiModel>())
        {

        }

        /// <summary>
        /// Constructor used by Newtonsoft during deserialization.
        /// </summary>
        [JsonConstructor]
        private AiProvider(
            string id,
            string name,
            string protocol,
            string icon,
            string iconColor,
            string apiKey,
            string baseUrl,
            bool isEnabled,
            bool? needApiKey,
            bool? isLocal,
            string modelEndPoint,
            List<AiModel> models)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException(@"Provider id cannot be empty.", nameof(id));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(@"Provider name cannot be empty.", nameof(name));

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException(@"BaseUrl cannot be empty.", nameof(baseUrl));

            if (string.IsNullOrWhiteSpace(protocol))
                throw new ArgumentException(@"protocol cannot be empty.", nameof(protocol));

            Id = id;
            Name = name;
            Protocol = protocol;
            Icon = string.IsNullOrWhiteSpace(icon)
                ? GetDefaultIcon(id, name, protocol)
                : icon;
            IconColor = string.IsNullOrWhiteSpace(iconColor)
                ? "#000000"
                : iconColor;
            ApiKey = apiKey ?? "";
            BaseUrl = baseUrl ?? "";
            IsEnabled = isEnabled;
            NeedApiKey = needApiKey ?? true;
            IsLocal = isLocal ?? false;
            ModelEndPoint = string.IsNullOrWhiteSpace(modelEndPoint)
                ? "/models"
                : modelEndPoint;

            _models = models ?? new List<AiModel>();
        }

        private static string GetDefaultIcon(string id, string name, string protocol)
        {
            if (string.Equals(id, "ollama", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(protocol, "ollama", StringComparison.OrdinalIgnoreCase))
                return "ollama";

            if (string.Equals(id, "openai", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(id, "openAI", StringComparison.OrdinalIgnoreCase))
                return "openai";

            return "puzzle";
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