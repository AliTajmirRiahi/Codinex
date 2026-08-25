using Codinex.Core.Models;
using Codinex.Core.Workspace.Prompt;

namespace Codinex.Core.Interfaces.Chat
{
    public interface IChatMessageBuilder
    {
        ChatMessageBuildResult Build(ChatMessageBuildRequest request, PromptContext promptContext);
    }
}