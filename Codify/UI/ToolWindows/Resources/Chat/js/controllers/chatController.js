/**
 * ChatController
 * Handles user interaction and coordinates between
 * the view layer and the service layer.
 */

import { appendMessage, createStreamingMessage } from '../views/chatView.js';
import { modelDropDownView } from '../views/modelDropDownView.js';
import { aiService } from '../services/aiService.js';
import { getState, setLoading } from '../state/appState.js';
import { EVENTS } from '../constants/events.js';


/**
 * Initialize chat controller
 * @param {Object} bridge - Communication bridge with VSCode extension host
 */
export function initChatController(bridge) {

    modelDropDownView.initEventHandlers();

    const input = document.getElementById('userInput');
    const sendBtn = document.getElementById('sendBtn');

    if (!input || !sendBtn) {
        console.warn('Chat input or send button not found');
        return;
    }

    /**
     * Send button click
     */
    sendBtn.addEventListener('click', handleSend);

    /**
     * Enter key send
     */
    input.addEventListener('keydown', (event) => {

        if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault();
            handleSend();
        }

    });


    /**
     * Main send handler
     */
    async function handleSend() {

        const text = input.value.trim();

        if (!text) return;

        const state = getState();

        // Prevent sending while AI is generating
        if (state.isLoading) return;

        // Clear input
        input.value = '';

        // Show user message immediately
        appendMessage(text, 'user');

        // Set loading state
        setLoading({ isLoading: true });

        try {

            // Create streaming UI container
            const streamingEl = createStreamingMessage();

            // Send message to AI
            const response = await aiService.sendMessage(text, bridge);

            // Update UI
            streamingEl.innerText = response;

        } catch (error) {

            console.error('AI request failed:', error);

            appendMessage('⚠️ Error generating response.', 'assistant');

        } finally {

            setLoading({ isLoading: false });
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
            modelDropDownView.renderProvider(appState.provider, appState.selectedModels);
        },
    };
}
