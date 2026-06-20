/**
 * ChatController
 * Handles user interaction and coordinates between
 * the view layer and the service layer.
 */
import { $ } from '../utils/dom.js';
import { createStreamingMessage, chatView } from '../views/chatView.js';
import { chatListView } from '../views/chatListView.js';
import { aiService } from '../services/aiService.js';
import { getState, setLoading, setCurrentModel } from '../state/appState.js';
import { EVENTS } from '../constants/events.js';
import { STATICS } from '../constants/statics.js';
import { reportError } from '../../../Shared/bridge/errorReporter.js'

/**
 * Initialize chat controller
 * @param {Object} transport - Communication transport with VS extension host
 */
export function initChatController(transport) {

    chatView.initialize(handleSend, onModelSelected);

    chatListView.initialize(onChatSelected, handleNewChat);

    function onModelSelected(model) {
        var appState = getState();
        const data = {
            providerId: appState.provider.id,
            modelId: model.id
        };
        transport.send(EVENTS.SELECT_MODEL, data);
    }

    function onChatSelected(chat) {
        var appState = getState();
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
            const response = await aiService.sendMessage(text, transport);

        } catch (error) {

            reportError(error, "chatController");

            chatView.appendErrorMessage(STATICS.GENERIC_CHAT_ERROR);

        }
    }

    async function handleNewChat() {
        clearChatContainer();
        transport.send(EVENTS.NEW_CHAT);
    }

    function clearChatContainer(){
        const chatElements = document.getElementsByClassName('chat-message');
        while (chatElements.length > 0) {
            const parent = chatElements[0].parentElement;
            parent.removeChild(chatElements[0]);
        }
    }
    /**
     * Returns public methods to allow external interaction with the controller
     */
    return {
        /**
         * Allows external components to programmatically trigger a message
         */
        sendMessage: (text) => {
            input.value = text;
            handleSend();
        },

        /**
         * Updates the active model from outside (e.g., when loading saved settings)
         */
        setActiveModel: (modelId) => {
        },

        /**
         * Clears the chat UI if needed
         */
        clearChat: () => {
            clearChatContainer();
        },

        renderCurrentProvider: () => {
            var appState = getState();
            chatView.renderModelMenu(appState.selectedModels, appState.currentModel.id);
        },

        renderChatList: () => {
            var appState = getState();
            chatListView.renderChatListMenu(appState.chatList, appState.currentChat.id);
        },

        handleAIResponse: (payload) => {
            // Show AI message
            setLoading(false);
            chatView.appendMessage(payload, 'assistant');
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
