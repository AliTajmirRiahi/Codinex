using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Codify.Core.Models
{

    public enum AiProviderFamily
    {
        GapGpt = 0,
        OpenAi = 1,
        Anthropic = 2,
        GoogleGemini = 3,
        Custom = 4,
        NaN = -1
    }
    public class AiModel
    {
        // Unique model identifier (e.g. "gpt-4o")
        public string Id { get; }

        // Display name (e.g. "GPT-4o")
        public string Name { get; private set; }

        // Maximum supported token limit
        public int TokenLimit { get; private set; }

        // Indicates if the model supports image input/output
        public bool SupportsImages { get; private set; }

        // Indicates if the model supports tool/function calling
        public bool SupportsTools { get; private set; }

        // Indicates if the model is selected
        public bool IsSelected { get; private set; } = false;

        public bool IsCurrent { get; private set; } = false;

        [JsonConverter(typeof(StringEnumConverter))]
        public AiProviderFamily Family { get; set; }

        /// <summary>
        /// Constructor enforces required invariants.
        /// </summary>
        public AiModel(
            string id,
            string name,
            int tokenLimit,
            bool supportsImages,
            bool supportsTools,
            bool isSelected,
            bool isCurrent)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException(@"Model id cannot be empty.", nameof(id));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(@"Model name cannot be empty.", nameof(name));

            if (tokenLimit <= 0)
                throw new ArgumentException(@"Token limit must be greater than zero.", nameof(tokenLimit));

            Id = id;
            Name = name;
            TokenLimit = tokenLimit;
            SupportsImages = supportsImages;
            SupportsTools = supportsTools;
            IsSelected = isSelected;
            IsCurrent = isCurrent;
        }

        /// <summary>
        /// Updates the display name.
        /// </summary>
        public void Rename(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException(@"Model name cannot be empty.", nameof(newName));

            Name = newName;
        }

        /// <summary>
        /// Updates token limit.
        /// </summary>
        public void UpdateTokenLimit(int newLimit)
        {
            if (newLimit <= 0)
                throw new ArgumentException(@"Token limit must be greater than zero.", nameof(newLimit));

            TokenLimit = newLimit;
        }

        /// <summary>
        /// Enables or disables image capability.
        /// </summary>
        public void SetImageSupport(bool enabled)
        {
            SupportsImages = enabled;
        }

        /// <summary>
        /// Enables or disables tool calling capability.
        /// </summary>
        public void SetToolSupport(bool enabled)
        {
            SupportsTools = enabled;
        }

        /// <summary>
        /// Selects the model, making it active.
        /// </summary>
        public void Select()
        {
            IsSelected = true;
        }
        /// <summary>
        /// Deselects the model, making it inactive.
        /// </summary>
        public void DeSelect()
        {
            IsSelected = false;
        }

        /// <summary>
        /// Make current model. This is the model that will be used for generating responses in the chat. Only one model can be current at a time per provider.
        /// </summary>
        public void MarkAsCurrent() 
        {
            IsCurrent = true;
        }
        /// <summary>
        /// Sets the current state to inactive, indicating that the current item is no longer selected or active.
        /// </summary>
        public void ClearCurrent() 
        {
            IsCurrent = false; 
        }
    }

}