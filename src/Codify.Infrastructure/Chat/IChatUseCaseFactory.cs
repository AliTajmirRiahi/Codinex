using Codify.Core.UseCases;

namespace Codify.Infrastructure.Chat
{
    public interface IChatUseCaseFactory
    {
        ISendChatMessageUseCase Create();
    }

}
