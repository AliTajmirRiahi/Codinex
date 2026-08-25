using System.Collections.Generic;
using Codinex.Core.Models.Chat;

namespace Codinex.Core.Interfaces.Chat
{
    /// <summary>
    /// Reduces the size of conversation history sent to AI providers by hiding
    /// tool-result content that is either superseded by a later, identical-target
    /// call or old enough to fall outside the recent tool-result window.
    /// </summary>
    public interface IToolHistoryCompactor
    {
        IReadOnlyList<ChatMessage> Compact(IReadOnlyList<ChatMessage> history);
    }
}
