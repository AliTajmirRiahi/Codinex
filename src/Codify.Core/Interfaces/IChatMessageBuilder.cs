using Codify.Core.Models;

namespace Codify.Core.Interfaces
{
    public interface IChatMessageBuilder
    {
        ChatMessageBuildResult Build(ChatMessageBuildRequest request);
    }
}