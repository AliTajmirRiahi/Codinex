using Codinex.Core.Models;
using Codinex.Core.Workspace.Prompt;

namespace Codinex.Core.Interfaces
{
    public interface IPromptProfiler
    {
        PromptProfileResult Profile(PromptContext context);
    }
}
