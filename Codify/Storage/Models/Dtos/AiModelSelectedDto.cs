
using System.Collections.Generic;

namespace Codify.Storage.Models.Dtos
{
    public class AiModelSelectedDto
    {
        public string ProviderId { get; set; } = ""; // "gapgpt", "openai"
        public string ModelId { get; set; } = ""; 
    }
}
