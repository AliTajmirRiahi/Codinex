/**
 * ChatController
 * Handles user interaction and coordinates between
 * the view layer and the service layer.
 */

import { appendMessage, createStreamingMessage } from '../views/chatView.js';
import { DropDownView } from '../views/dropDownView.js';
import { aiService } from '../services/aiService.js';
import { getState, setLoading } from '../state/appState.js';
import { EVENTS } from '../constants/events.js';


/**
 * Initialize chat controller
 * @param {Object} bridge - Communication bridge with VSCode extension host
 */
export function initChatController(bridge) {

    // Initialize
    const modelDropDown = new DropDownView({
        containerId: 'model-dropdown-menu-container',
        menuId: 'model-dropdown-menu',
        menuButtonId: 'model-selector-btn',
        itemTemplate: (item, isActive) => {
            const option = document.createElement('div');
            option.className = `model-option ${isActive ? 'active' : ''}`;
            option.dataset.value = item.id;

            option.innerHTML = `
                    <div class="model-info">
                        <codify-icon name="lightning" class="low-vis"></codify-icon>
                        <span>${item.name}</span>
                    </div>
                    <span class="multiplier">${item.multiplier || '1x'}</span>`;
            return option;
        },
        onItemSelect: (model) => {
            console.log('Selected model:', model.name);
            // Update app state or trigger API change
        }
    });

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
            modelDropDown.render(appState.selectedModels, '');
        },
    };
}
