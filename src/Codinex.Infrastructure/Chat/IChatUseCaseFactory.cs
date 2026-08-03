using Codinex.Core.UseCases;

namespace Codinex.Infrastructure.Chat
{
    public interface IChatUseCaseFactory
    {
        ISendChatMessageUseCase Create();
    }

}
