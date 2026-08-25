using Codinex.Core.Models.Chat;
using Codinex.Core.Workspace.Prompt;

namespace Codinex.Core.Interfaces.Chat
{
    public interface IChatMessageBuilder
    {
        ChatMessageBuildResult Build(ChatMessageBuildRequest request, PromptContext promptContext);
    }
}