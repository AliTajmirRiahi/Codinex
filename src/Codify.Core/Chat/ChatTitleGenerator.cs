namespace Codify.Core.Chat
{
    /// <summary>
    /// Generates a short title for a chat session based on the first user message.
    /// This avoids calling an AI model and keeps title generation deterministic.
    /// </summary>
    public static class ChatTitleGenerator
    {
        /// <summary>
        /// Generates a title using the first user message.
        /// The title is trimmed and limited to a maximum length.
        /// </summary>
        public static string Generate(string firstMessage)
        {
            if (string.IsNullOrWhiteSpace(firstMessage))
                return "New Chat";

            var cleaned = firstMessage
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Trim();

            const int maxLength = 50;

            if (cleaned.Length <= maxLength)
                return cleaned;

            return cleaned.Substring(0, maxLength) + "...";
        }
    }
}