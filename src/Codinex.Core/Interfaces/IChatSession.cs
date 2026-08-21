using System.Collections.Generic;
using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces
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
            string modelName);
        IReadOnlyList<ChatMessage> GetRecentMessages(int count);
    }
}
