using Codify.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Codify.Core.Interfaces
{
    public interface IChatSession
    {
        string SessionId { get; }
        IReadOnlyList<ChatMessage> Messages { get; }
        Task LoadAsync(string id);
        Task<bool> SaveAsync();
        ChatMessage AddUserMessage(string content, ChatMessageRequestContext context);
        ChatMessage AddAssistantMessage(string content);
        IReadOnlyList<ChatMessage> GetRecentMessages(int count);
    }
}
