namespace Codinex.Core.Interfaces
{
    /// <summary>
    /// Maps a Codinex chat session to the OpenCode server session created for it, so a single
    /// OpenCode session is reused across turns of the same conversation instead of recreated per message.
    /// </summary>
    public interface IOpenCodeSessionStore
    {
        bool TryGetSessionId(string conversationId, out string openCodeSessionId);

        void SetSessionId(string conversationId, string openCodeSessionId);
    }
}
