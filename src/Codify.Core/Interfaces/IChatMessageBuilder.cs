using Codify.Core.Models;
using Codify.Core.Workspace.Prompt;

namespace Codify.Core.Interfaces
{
    public interface IChatMessageBuilder
    {
        ChatMessageBuildResult Build(ChatMessageBuildRequest request, PromptContext promptContext);
    }
}