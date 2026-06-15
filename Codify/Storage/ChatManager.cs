using Codify.Core.Models;
using Codify.Storage.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Codify.Storage
{
    /// <summary>
    /// Manages chat sessions persistence and lifecycle.
    /// </summary>
    public class ChatManager
    {
        private readonly IStorageService _storage;

        public ChatManager(IStorageService storage)
        {
            _storage = storage;
        }

        /// <summary>
        /// Creates a new chat session
        /// </summary>
        public async Task<ChatSessionDocument> CreateChatAsync(string providerId,string modelId)
        {
            var id = Guid.NewGuid().ToString();

            var doc = new ChatSessionDocument
            {
                Id = id,
                Title = "New Chat",
                ProviderId = providerId,
                ModelId = modelId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await SaveChatAsync(doc);

            return doc;
        }

        /// <summary>
        /// Returns the file path for a chat session
        /// </summary>
        private string GetChatPath(string chatId)
        {
            return Path.Combine(StoragePaths.Chats, $"chat-{chatId}.json");
        }

        /// <summary>
        /// Loads a chat session from disk
        /// </summary>
        public async Task<ChatSessionDocument?> LoadChatAsync(string chatId)
        {
            var path = GetChatPath(chatId);

            if (!File.Exists(path))
                return null;

            return await _storage.LoadAsync<ChatSessionDocument>(path);
        }

        /// <summary>
        /// Saves a chat session
        /// </summary>
        public async Task SaveChatAsync(ChatSessionDocument chat)
        {
            chat.UpdatedAt = DateTime.UtcNow;

            var path = GetChatPath(chat.Id);

            await _storage.SaveAsync(path, chat);
        }

        /// <summary>
        /// Deletes a chat session
        /// </summary>
        public void DeleteChat(string chatId)
        {
            var path = GetChatPath(chatId);

            if (File.Exists(path))
                File.Delete(path);
        }

        /// <summary>
        /// Returns all chats ordered by update date
        /// </summary>
        public async Task<List<ChatSessionDocument>> GetAllChatsAsync()
        {
            if (!Directory.Exists(StoragePaths.Chats))
                return new List<ChatSessionDocument>();

            var files = Directory.GetFiles(StoragePaths.Chats, "chat-*.json");

            var chats = new List<ChatSessionDocument>();

            foreach (var file in files)
            {
                var chat = await _storage.LoadAsync<ChatSessionDocument>(file);

                if (chat != null)
                    chats.Add(chat);
            }

            return chats
                .OrderByDescending(x => x.UpdatedAt)
                .ToList();
        }

        /// <summary>
        /// Adds a message to a chat session
        /// </summary>
        public async Task AddMessageAsync(string chatId, ChatMessage message)
        {
            var chat = await LoadChatAsync(chatId);

            if (chat == null)
                return;

            chat.Messages.Add(message);

            await SaveChatAsync(chat);
        }

        /// <summary>
        /// Returns the last N messages for AI context
        /// </summary>
        public async Task<List<ChatMessage>> GetRecentMessages(string chatId, int count)
        {
            var chat = await LoadChatAsync(chatId);

            if (chat == null)
                return new List<ChatMessage>();

            return chat.Messages
                .Take(count)
                .ToList();
        }
    }
}
