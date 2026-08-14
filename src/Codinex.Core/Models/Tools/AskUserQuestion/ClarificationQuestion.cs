using System.Collections.Generic;

namespace Codinex.Core.Models.Tools.AskUserQuestion;

public sealed class ClarificationQuestion
{
    public string Header { get; set; }

    public string Question { get; set; }

    public List<ClarificationOption> Options { get; set; } = new();
}
