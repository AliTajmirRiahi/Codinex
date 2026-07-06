using Codify.Core.Models;

namespace Codify.Core.Chat
{
    public interface IChatMessageBuilder
    {
        ChatMessageBuildResult Build(ChatMessageBuildRequest request);
    }
}