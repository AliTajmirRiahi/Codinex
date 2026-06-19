/**
 * ChatController
 * Handles user interaction and coordinates between
 * the view layer and the service layer.
 */
import { $ } from '../utils/dom.js';
import { createStreamingMessage, chatView } from '../views/chatView.js';
import { aiService } from '../services/aiService.js';
import { getState, setLoading, setCurrentModel } from '../state/appState.js';
import { EVENTS } from '../constants/events.js';


/**
 * Initialize chat controller
 * @param {Object} transport - Communication transport with VS extension host
 */
export function initChatController(transport) {

    chatView.initialize(handleSend, onModelSelected);

    function onModelSelected(model) {
        var appState = getState();
        const data = {
            providerId: appState.provider.id,
            modelId: model.id
        };
        transport.send(EVENTS.SELECT_MODEL, data);
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

            console.error('AI request failed:', error);

            chatView.appendMessage('⚠️ Error generating response.', 'assistant');

        } finally {

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
            const container = document.getElementById('chatContainer');
            if (container) container.innerHTML = '';
        },

        renderCurrentProvider: () => {
            var appState = getState();
            chatView.renderModelMenu(appState.selectedModels, appState.currentModel.id);
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
        }
    };
}
