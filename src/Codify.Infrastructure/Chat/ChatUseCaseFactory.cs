using Codify.Core.Conversation;
using Codify.Core.Interfaces;
using Codify.Core.UseCases;
using Codify.Infrastructure.AI.Providers;
using Codify.Storage;
using System;

namespace Codify.Infrastructure.Chat
{
    /// <summary>
    /// Provides a factory for creating chat use case instances that enable sending chat messages using the currently
    /// active AI provider, model, and chat session.
    /// </summary>
    /// <remarks>This factory requires that an active provider, model, and chat session are available;
    /// otherwise, an exception is thrown when attempting to create a use case. The factory supports multiple AI
    /// providers, such as GapGpt and ChatGpt, and will throw a NotSupportedException if an unsupported provider is
    /// selected. This class is intended to centralize the creation logic for chat message use cases, ensuring that all
    /// necessary dependencies are present and correctly configured.</remarks>
    public class ChatUseCaseFactory(
        ProviderManager providerManager,
        ChatSessionService sessionService,
        IJsonSerializer serializer,
        IErrorHandler errorHandler,
        IChatMessageBuilder chatMessageBuilder,
        IConversationEngine conversationEngine,
        IOpenAiCompatibleClient openAiCompatibleClient)
        : IChatUseCaseFactory
    {
        private readonly ProviderManager _providerManager = providerManager;

        public ISendChatMessageUseCase Create()
        {
            var provider = _providerManager.ActiveProvider
                           ?? throw new InvalidOperationException("Active Provider not found.");

            var model = _providerManager.ActiveModel
                        ?? throw new InvalidOperationException("Active Model not found.");

            var session = sessionService.ActiveSession
                          ?? throw new InvalidOperationException("No active chat session.");

            IAiProvider aiProvider = provider.Id.ToLower() switch
            {
                "gapgpt" => new OpenAiCompatibleProvider(serializer, _providerManager, openAiCompatibleClient),
                "chatgpt" => new OpenAiCompatibleProvider(serializer, _providerManager, openAiCompatibleClient),
                _ => throw new NotSupportedException($"Provider {provider.Name} is not supported.")
            };

            return new SendChatMessageUseCase(aiProvider, session, errorHandler, chatMessageBuilder, conversationEngine);
        }
    }

}
