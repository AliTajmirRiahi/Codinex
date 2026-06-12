using System;

namespace Codify.Storage.Models
{
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

        /// <summary>
        /// Constructor enforces required invariants.
        /// </summary>
        public AiModel(
            string id,
            string name,
            int tokenLimit,
            bool supportsImages,
            bool supportsTools,
            bool isSelected)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Model id cannot be empty.", nameof(id));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Model name cannot be empty.", nameof(name));

            if (tokenLimit <= 0)
                throw new ArgumentException("Token limit must be greater than zero.", nameof(tokenLimit));

            Id = id;
            Name = name;
            TokenLimit = tokenLimit;
            SupportsImages = supportsImages;
            SupportsTools = supportsTools;
            IsSelected = isSelected;
        }

        /// <summary>
        /// Updates the display name.
        /// </summary>
        public void Rename(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Model name cannot be empty.", nameof(newName));

            Name = newName;
        }

        /// <summary>
        /// Updates token limit.
        /// </summary>
        public void UpdateTokenLimit(int newLimit)
        {
            if (newLimit <= 0)
                throw new ArgumentException("Token limit must be greater than zero.", nameof(newLimit));

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
        /// 
        /// </summary>
        public void Selected()
        {
            IsSelected = true;
        }
        /// <summary>
        /// 
        /// </summary>
        public void DeSelected()
        {
            IsSelected = false;
        }
    }

}