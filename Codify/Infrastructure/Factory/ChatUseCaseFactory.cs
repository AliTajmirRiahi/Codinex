using System;
using Codify.Core.Abstractions;
using Codify.Core.Chat;
using Codify.Core.UseCases;
using Codify.Infrastructure.AiProviders;
using Codify.Infrastructure.ChatSessions;
using Codify.Infrastructure.Errors;
using Codify.Storage;

namespace Codify.Infrastructure.Factory
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
    public class ChatUseCaseFactory : IChatUseCaseFactory
    {
        private readonly ProviderManager _providerManager;
        private readonly ChatSessionService _sessionService;
        private readonly IJsonSerializer _serializer;
        private readonly IErrorHandler _errorHandler;
        private readonly IChatMessageBuilder _chatMessageBuilder;

        public ChatUseCaseFactory(
            ProviderManager providerManager,
            ChatSessionService sessionService,
            IJsonSerializer serializer,
            IErrorHandler errorHandler,
            IChatMessageBuilder chatMessageBuilder)
        {
            _providerManager = providerManager;
            _sessionService = sessionService;
            _serializer = serializer;
            _errorHandler = errorHandler;
            _chatMessageBuilder = chatMessageBuilder;
        }

        public ISendChatMessageUseCase Create()
        {
            var provider = _providerManager.ActiveProvider
                           ?? throw new InvalidOperationException("Active Provider not found.");

            var model = _providerManager.ActiveModel
                        ?? throw new InvalidOperationException("Active Model not found.");

            var session = _sessionService.ActiveSession
                          ?? throw new InvalidOperationException("No active chat session.");

            IAiProvider aiProvider = provider.Id.ToLower() switch
            {
                "gapgpt" => new GapGptProvider(_serializer, _providerManager),
                "chatgpt" => new OpenAiProvider(), 
                _ => throw new NotSupportedException($"Provider {provider.Name} is not supported.")
            };

            return new SendChatMessageUseCase(aiProvider, session, _errorHandler, _chatMessageBuilder);
        }
    }

}
