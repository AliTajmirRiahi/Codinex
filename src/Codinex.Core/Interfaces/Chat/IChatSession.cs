using System.Collections.Generic;
using System.Threading.Tasks;
using Codinex.Core.Models.Chat;

namespace Codinex.Core.Interfaces.Chat
{
    public interface IChatSession
    {
        string SessionId { get; }
        IReadOnlyList<ChatMessage> Messages { get; }
        Task LoadAsync(string id);
        Task<bool> SaveAsync();
        ChatMessage AddUserMessage(string content, ChatMessageRequestContext context);
        ChatMessage AddAssistantMessage(
            string content,
            string providerId,
            string modelId,
            string providerName,
            string modelName,
            bool isPreprocessorAnswer = false);
        IReadOnlyList<ChatMessage> GetRecentMessages(int count);
    }
}
