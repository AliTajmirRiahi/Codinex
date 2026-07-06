using Codify.Core.Abstractions;
using Codify.Core.Chat;
using Codify.Core.Models;
using Codify.Infrastructure.Commons;
using Codify.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Codify.Infrastructure.ChatSessions
{
    /// <summary>
    /// In-memory implementation of IChatSession.
    /// 
    /// This class is responsible for:
    /// - Holding chat messages
    /// - Managing message history
    /// - Providing a memory window (e.g., last 10 messages)
    /// 
    /// NOTE:
    /// LoadAsync and SaveAsync are currently no-op (in-memory only).
    /// They can later be extended to persist data using a storage service.
    /// </summary>
    public sealed class ChatSession : IChatSession
    {
        private readonly ChatManager _chatManager;

        private List<ChatMessage> _messages;

        public string SessionId { get; private set; }

        public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();

        public ChatSession(ChatManager chatManager)
        {
            _chatManager = chatManager;
            _messages = new List<ChatMessage>();
            SessionId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Loads a session by ID.
        /// </summary>
        public async Task LoadAsync(string sessionId)
        {
            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));

            var chatData = await ChatSessionDocumentAsync(sessionId);

            _messages = chatData?.Messages;
        }

        /// <summary>
        /// Saves the current session state.
        /// </summary>
        public async Task<bool> SaveAsync()
        {
            var chatData = await ChatSessionDocumentAsync(SessionId);

            var isTitleChanged = false;

            if (_messages.Count > 0 && chatData.Title == Statics.NewChatTitle)
            {
                chatData.Title = ChatTitleGenerator.Generate(_messages.First().Content);
                isTitleChanged = true;
            }

            chatData.Messages = _messages;

            await _chatManager.SaveChatAsync(chatData);

            return isTitleChanged;
        }

        /// <summary>
        /// Adds a user message to the session.
        /// </summary>
        public ChatMessage AddUserMessage(string content, ChatMessageRequestContext context)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new Exception();

            var msg = new ChatMessage
            {
                Role = "user",
                Content = content,
                Context = ChatMessageRequestContext.CreateChatMessageRequestContextWithoutMetaData(context),
                CreatedAt = DateTime.UtcNow
            };

            _messages.Add(msg);

            return msg;
        }

        /// <summary>
        /// Adds an assistant message to the session.
        /// </summary>
        public ChatMessage AddAssistantMessage(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new Exception();

            var msg = new ChatMessage
            {
                Role = "assistant",
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _messages.Add(msg);

            return msg;
        }

        /// <summary>
        /// Returns the last N messages ordered chronologically.
        /// </summary>
        public IReadOnlyList<ChatMessage> GetRecentMessages(int count)
        {
            if (count <= 0)
                return new List<ChatMessage>();

            return _messages
                .OrderBy(m => m.CreatedAt)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Asynchronously retrieves the chat session document associated with the specified session ID.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the chat session to load. Cannot be null or empty.</param>
        /// <returns>A <see cref="ChatSessionDocument"/> representing the loaded chat session. Returns null if the session does
        /// not exist.</returns>
        /// <exception cref="InvalidOperationException">Thrown if a chat session with the specified ID already exists.</exception>
        private async Task<ChatSessionDocument> ChatSessionDocumentAsync(string sessionId)
        {
            var chatData = await _chatManager.LoadChatAsync(sessionId);

            return chatData ?? throw new InvalidOperationException($"Chat session with ID {sessionId} already exists.");
        }
    }
}
