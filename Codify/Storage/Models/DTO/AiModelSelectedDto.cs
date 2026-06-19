
using System.Collections.Generic;

namespace Codify.Storage.Models.DTO;

public class AiModelSelectedDto
{
    public string ProviderId { get; set; } = ""; // "gapgpt", "openai"
    public string ModelId { get; set; } = ""; 
}