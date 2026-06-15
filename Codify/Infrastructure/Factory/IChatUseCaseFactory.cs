using Codify.Core.UseCases;

namespace Codify.Infrastructure.Factory
{
    public interface IChatUseCaseFactory
    {
        ISendChatMessageUseCase Create();
    }

}
