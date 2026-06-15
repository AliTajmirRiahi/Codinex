using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codify.Storage.Models;

namespace Codify.Core.Models
{
    public class ChatMessage
    {
        public string Message { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public AiProviderFamily Family { get; set; } = AiProviderFamily.NaN;

        /// <summary>
        /// Unique identifier for the provider (e.g., "gapgpt", "ollama", "azure").
        /// </summary>
        public AiProvider Provider { get; set; }

        /// <summary>
        /// The specific AI model being used (e.g., "gpt-4o", "llama3-8b"). This is important for determining capabilities and token limits.
        /// </summary>
        public AiModel Model { get; set; }
    }
}
