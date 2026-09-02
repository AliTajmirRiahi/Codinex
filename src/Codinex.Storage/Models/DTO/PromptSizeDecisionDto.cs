namespace Codinex.Storage.Models.DTO;

public class PromptSizeDecisionDto
{
    public string RequestId { get; set; }

    // true = Continue (send anyway), false = Stop (abort the send).
    public bool Proceed { get; set; }
}
