using System;
using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Tools;
using Codinex.Core.UseCases;
using Codinex.Core.Workspace.Prompt;
using Codinex.Storage.Managers;

namespace Codinex.Infrastructure.Chat
{
    /// <summary>
    /// Provides a factory for creating chat use case instances that enable sending chat messages using the runtime
    /// AI provider router, current model, and active chat session.
    /// </summary>
    [AutoDiRegister(Modules.Chat, RegistrationOrder.Infrastructure)]
    public class ChatUseCaseFactory(
        ProviderManager providerManager,
        ChatSessionService sessionService,
        IAiProviderRouter aiProviderRouter,
        IErrorHandler errorHandler,
        IChatMessageBuilder chatMessageBuilder,
        IConversationEngine conversationEngine,
        IWorkspaceContextBuilder workspaceContextBuilder,
        IAiToolRegistry toolRegistry)
        : IChatUseCaseFactory
    {
        public ISendChatMessageUseCase Create()
        {
            _ = providerManager.ActiveProvider
                ?? throw new InvalidOperationException("Active Provider not found.");

            _ = providerManager.ActiveModel
                ?? throw new InvalidOperationException("Active Model not found.");

            var session = sessionService.ActiveSession
                          ?? throw new InvalidOperationException("No active chat session.");

            _ = aiProviderRouter.GetCurrentProvider();

            return new SendChatMessageUseCase(
                session,
                errorHandler,
                chatMessageBuilder,
                conversationEngine,
                workspaceContextBuilder,
                aiProviderRouter,
                toolRegistry);
        }
    }

}
