using System.Collections.Generic;
using Codinex.Core.Models.Tools.AskUserQuestion;

namespace Codinex.Storage.Models.DTO;

public class AskUserAnswerDto
{
    public string RequestId { get; set; }

    public List<ClarificationAnswer> Answers { get; set; } = new();
}
