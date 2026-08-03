using Codinex.Core.Models;
using Codinex.Core.Workspace.Prompt;

namespace Codinex.Core.Interfaces
{
    public interface IChatMessageBuilder
    {
        ChatMessageBuildResult Build(ChatMessageBuildRequest request, PromptContext promptContext);
    }
}