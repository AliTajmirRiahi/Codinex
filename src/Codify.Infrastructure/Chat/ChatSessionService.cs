using System;
using System.Linq;
using System.Threading.Tasks;
using Codify.Core.Interfaces;
using Codify.Storage;

namespace Codify.Infrastructure.Chat
{
    public class ChatSessionService(ChatManager chatManager, ProviderManager providerManager)
    {
        /// <summary>
        /// Gets the currently active chat session.
        /// </summary>
        /// <remarks>This property is read-only and is set internally. It returns null if no chat session
        /// is currently active.</remarks>
        public IChatSession ActiveSession { get; private set; }

        /// <summary>
        /// Initializes the chat session asynchronously by loading an existing empty chat session if available, or
        /// creating a new session if none exist.
        /// </summary>
        /// <remarks>If there are existing chat sessions, the method attempts to load the first session
        /// that does not contain any messages. If no such session exists, a new chat session is created using the
        /// currently active provider and model. This method should be called before interacting with chat sessions to
        /// ensure a valid session is available.</remarks>
        /// <returns>A task that represents the asynchronous initialization operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if there is no active provider or active model available when attempting to create a new chat
        /// session.</exception>
        public async Task InitializeAsync()
        {
            var chats = await chatManager.GetAllChatsAsync();

            var emptyChat = chats.FirstOrDefault(c => c.Messages == null || !c.Messages.Any());

            if (emptyChat != null)
            {
                await LoadSessionAsync(emptyChat.Id);
                return;
            }

            var providerId = providerManager.ActiveProvider?.Id ?? throw new InvalidOperationException("No active provider found.");
            var modelId = providerManager.ActiveModel?.Id ?? throw new InvalidOperationException("No active model found.");

            await CreateNewSessionAsync(providerId, modelId);
        }
        /// <summary>
        /// Asynchronously creates a new chat session using the specified provider and model identifiers.
        /// </summary>
        /// <remarks>This method creates a new chat session and loads it based on the generated document
        /// ID. Ensure that the specified provider and model identifiers correspond to valid configurations.</remarks>
        /// <param name="providerId">The unique identifier of the chat provider to use for the new session. Cannot be null or empty.</param>
        /// <param name="modelId">The unique identifier of the model to associate with the new chat session. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task CreateNewSessionAsync(string providerId, string modelId)
        {
            var doc = await chatManager.CreateChatAsync(providerId, modelId);

            await LoadSessionAsync(doc.Id);
        }
        /// <summary>
        /// Asynchronously loads a chat session using the specified session identifier and sets it as the active
        /// session.
        /// </summary>
        /// <remarks>After the session is loaded, it becomes the active session for subsequent
        /// interactions.</remarks>
        /// <param name="sessionId">The unique identifier of the session to load. This value must not be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation of loading the session.</returns>
        public async Task LoadSessionAsync(string sessionId)
        {
            var session = new ChatSession(chatManager);

            await session.LoadAsync(sessionId);

            ActiveSession = session;
        }
    }
}