using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Storage.Interfaces;
using Codinex.Storage.Models;
using Codinex.Storage.Services;

namespace Codinex.Storage.Managers
{
    /// <summary>
    /// Manages chat sessions persistence and lifecycle.
    /// </summary>
    [AutoDiRegister(Modules.Storage, RegistrationOrder.Foundation)]
    public class ChatManager(IStorageService storage, IFileSystem fileSystem, IWorkspaceContext workspaceContext, IConversationGroupManager conversationGroupManager)
    {
        /// <summary>
        /// Creates a new chat session
        /// </summary>
        public async Task<ChatSessionDocument> CreateChatAsync(string providerId, string modelId)
        {
            var id = Guid.NewGuid().ToString();

            var doc = new ChatSessionDocument
            {
                Id = id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsSelected = true
            };

            await DeselectAllChatsAsync();

            await SaveChatAsync(doc);

            return doc;
        }

        /// <summary>
        /// Returns the file path for a chat session
        /// </summary>
        private string GetChatPath(string chatId)
        {
            return StoragePaths.GetChatPath(workspaceContext.SolutionName, conversationGroupManager.CurrentGroup.GetId(), "chat-" + chatId);
        }

        /// <summary>
        /// Loads a chat session from disk
        /// </summary>
        public async Task<ChatSessionDocument> LoadChatAsync(string chatId)
        {
            var path = GetChatPath(chatId);

            if (!File.Exists(path))
                return null;

            return await storage.LoadAsync<ChatSessionDocument>(path);
        }

        /// <summary>
        /// Saves a chat session
        /// </summary>
        public async Task SaveChatAsync(ChatSessionDocument chat)
        {
            chat.UpdatedAt = DateTime.UtcNow;

            var path = GetChatPath(chat.Id);

            await storage.SaveAsync(path, chat);
        }

        /// <summary>
        /// Updates a chat session title
        /// </summary>
        public async Task<ChatSessionDocument> UpdateChatTitleAsync(string chatId, string title)
        {
            var chat = await LoadChatAsync(chatId);

            if (chat == null)
                return null;

            chat.Title = title;

            await SaveChatAsync(chat);

            return chat;
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
            if (!fileSystem.Directory.Exists(StoragePaths.GetGroupPath(workspaceContext.SolutionName, conversationGroupManager.CurrentGroup.GetId())))
                return [];

            var files = fileSystem.Directory.GetFiles(StoragePaths.GetGroupPath(workspaceContext.SolutionName, conversationGroupManager.CurrentGroup.GetId()), "chat-*.json");

            var chats = new List<ChatSessionDocument>();

            foreach (var file in files)
            {
                var chat = await storage.LoadAsync<ChatSessionDocument>(file);

                if (chat != null)
                    chats.Add(chat);
            }

            return chats
                .OrderByDescending(x => x.CreatedAt)
                .ToList();
        }

        /// <summary>
        /// Selects the active chat session and deselects all other chats in the current project.
        /// </summary>
        public async Task SelectChatAsync(string chatId)
        {
            if (string.IsNullOrWhiteSpace(chatId))
                throw new ArgumentException("Chat id is required.", nameof(chatId));

            var chats = await GetAllChatsAsync();
            var selectedChat = chats.FirstOrDefault(chat =>
                string.Equals(chat.Id, chatId, StringComparison.OrdinalIgnoreCase));

            if (selectedChat == null)
                throw new InvalidOperationException($"Chat session '{chatId}' was not found.");

            foreach (var chat in chats)
            {
                var shouldBeSelected = string.Equals(chat.Id, chatId, StringComparison.OrdinalIgnoreCase);

                if (chat.IsSelected == shouldBeSelected)
                    continue;

                chat.IsSelected = shouldBeSelected;
                await SaveChatAsync(chat);
            }
        }

        private async Task DeselectAllChatsAsync()
        {
            var chats = await GetAllChatsAsync();

            foreach (var chat in chats.Where(chat => chat.IsSelected))
            {
                chat.IsSelected = false;
                await SaveChatAsync(chat);
            }
        }

        /// <summary>
        /// Returns the last N messages for AI context
        /// </summary>
        public async Task<List<ChatMessage>> GetRecentMessages(string chatId, int count)
        {
            var chat = await LoadChatAsync(chatId);

            if (chat == null)
                return [];

            return chat.Messages
                .Take(count)
                .ToList();
        }
    }
}
