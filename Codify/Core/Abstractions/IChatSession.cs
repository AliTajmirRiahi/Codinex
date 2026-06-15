using Codify.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Codify.Core.Abstractions
{
    public interface IChatSession
    {
        string SessionId { get; }
        IReadOnlyList<ChatMessage> Messages { get; }
        Task LoadAsync(string id);
        Task SaveAsync();
        void AddUserMessage(string content);
        void AddAssistantMessage(string content);
        IReadOnlyList<ChatMessage> GetRecentMessages(int count);
    }
}
