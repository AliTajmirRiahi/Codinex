/**
 * ChatController
 * Handles user interaction and coordinates between
 * the view layer and the service layer.
 */
import { createStreamingMessage, chatView } from '../views/chatView.js';
import { chatListView } from '../views/chatListView.js';
import { aiService } from '../services/aiService.js';
import { getState, setLoading, setCurrentModel } from '../state/appState.js';
import { EVENTS } from '../constants/events.js';
import { STATICS } from '../constants/statics.js';
import { reportError } from '../../../Shared/bridge/errorReporter.js';
import { ComposerController } from '../controllers/composerController.js';


let activeStreamMessage = null;
let accumulatedText = '';

/**
 * Initialize chat controller
 * @param {Object} transport - Communication transport with VS extension host
 */
export function initChatController(transport) {

    chatView.initialize(handleSend, onModelSelected);

    const composerController = new ComposerController(chatView.composer);

    chatView.composer.setOnChange((ctx) => {
        composerController.handleInput(ctx);
    });

    chatListView.initialize(onChatSelected, handleNewChat, handleDeleteChat);

    function onModelSelected(model) {
        var appState = getState();
        const data = {
            providerId: appState.provider.id,
            modelId: model.id
        };
        transport.send(EVENTS.SELECT_MODEL, data);
    }

    function onChatSelected(chat) {
        const data = {
            chatId: chat.id
        };
        transport.send(EVENTS.SELECT_CHAT, data);
    }

    /**
     * Main send handler
     */
    async function handleSend(text) {
        if (!text) return;

        const state = getState();

        // Prevent sending while AI is generating
        if (state.isLoading) return;

        // Show user message immediately
        chatView.appendMessage(text, 'user');

        // Set loading state
        setLoading(true);

        try {
            // Send message to AI
            await aiService.sendMessage(text, transport);

        } catch (error) {
            setLoading(false);

            reportError(error, "chatController");

            chatView.appendErrorMessage(STATICS.GENERIC_CHAT_ERROR);

        }
    }

    async function handleNewChat() {
        chatView.clearMessages();
        transport.send(EVENTS.NEW_CHAT);
    }

    async function handleDeleteChat() {
        chatView.clearMessages();
        transport.send(EVENTS.DELETE_CHAT);
    }
    /**
     * Returns public methods to allow external interaction with the controller
     */
    return {
        /**
         * Updates the active model from outside (e.g., when loading saved settings)
         */
        setActiveModel: (modelId) => {
        },

        /**
         * Clears the chat UI if needed
         */
        clearChat: () => {
            chatView.clearMessages();
        },

        renderCurrentProvider: () => {
            const appState = getState();

            const currentModelId = appState.currentModel
                ? appState.currentModel.id
                : null;

            chatView.renderModelMenu(
                appState.selectedModels,
                currentModelId
            );
        },

        renderChatList: () => {
            const appState = getState();

            const currentChatId = appState.currentChat
                ? appState.currentChat.id
                : null;

            chatListView.renderChatListMenu(
                appState.chatList,
                currentChatId
            );
        },

        handleAIResponse: (payload) => {
            setLoading(false);

            // If streaming was active → finalize it
            if (activeStreamMessage) {

                // Use full accumulated text OR payload (depending on backend behavior)
                const finalText = payload || accumulatedText;

                chatView.finalizeMessage(activeStreamMessage, finalText);

                activeStreamMessage = null;
                accumulatedText = '';

                return;
            }

            // Non-stream fallback
            chatView.appendMessage(payload, 'assistant');
        },
        handleStreamChunk: (payload) => {
            // Stop loading spinner only on first chunk
            if (!activeStreamMessage) {
                activeStreamMessage = createStreamingMessage();
                accumulatedText = '';
            }

            accumulatedText += payload;

            chatView.updateMessage(activeStreamMessage, payload);
        },
        handleAIError: (payload) => {
            // Show AI Error
            setLoading(false);
            chatView.appendErrorMessage(payload);
        },
        navigateToChat: () => {
            clearChatContainer();
            chatListView.setCurrentChatName();
            var appState = getState();
            chatView.renderMessages(appState.currentChat.messages);
        },
        handleChatTitleChanged: () => {
            chatListView.setCurrentChatName();
        }
    };
}
