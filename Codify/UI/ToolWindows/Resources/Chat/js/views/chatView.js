/**
 * ChatView
 * Responsible only for rendering UI elements.
 * No business logic or AI communication should exist here.
 */

import { $, togglePanelDisable, togglePanelHidden } from '../utils/dom.js';
import { addMessage } from '../state/appState.js';
import { DropDownView } from '../views/dropDownView.js';
import { getState, setCurrentModel, setLoading, subscribe } from '../state/appState.js';
import { messageView } from '../views/messageView.js';
import { CodeRenderer } from "../../../Shared/components/code-renderer.js";

export const chatView = {

    initialize(handleSend, onModelSelected) {
        this.handleSend = handleSend;
        // Initialize
        this.modelDropDown = new DropDownView({
            containerId: 'model-dropdown-menu-container',
            menuId: 'model-dropdown-menu',
            menuButtonId: 'model-selector-btn',
            itemTemplate: (item, isActive) => {
                const option = document.createElement('div');
                option.className = `drop-option ${isActive ? 'active' : ''}`;
                option.dataset.value = item.id;

                option.innerHTML = `
                    <div class="drop-info">
                        <codify-icon name="lightning" class="low-vis"></codify-icon>
                        <span>${item.name}</span>
                    </div>
                    <span class="multiplier">${item.multiplier || '1x'}</span>`;
                return option;
            },
            onItemSelect: (model) => {
                onModelSelected(model);
                setCurrentModel(model);
                this.setCurrentModelName();
                return true;
            }
        });


        const input = $('#userInput');
        const sendBtn = $('#send-btn');
        const responseLoading = $('#response-loading');

        this.inputMinHeight = parseFloat(window.getComputedStyle(input).minHeight);

        if (!input || !sendBtn) {
            throw new Error("ChatView initialization failed: Missing required DOM elements.");  
            return;
        }

        /**
         * Send button click
         */
        sendBtn.addEventListener('click', () => {
            this.handleSendMessage(input);
        });
        /**
         * Enter key send
         */
        input.addEventListener('keydown', (event) => {

            if (event.key === 'Enter' && !event.shiftKey) {
                event.preventDefault();
                this.handleSendMessage(input);
            }

        });

        /**
        * change 
        */
        input.addEventListener('input', (e) => {
            input.style.height = 'auto';
            if (input.scrollHeight > 500)
                input.style.height = '500px';
            else
                input.style.height = (input.scrollHeight) + 'px';
            togglePanelDisable('#send-btn', e.target.value != '');
        }, false);

        subscribe(function (state) {
            togglePanelHidden('#response-loading', state.isLoading);
        })
    },

    getInputMessage(input) {
        const text = input.value.trim();

        if (!text) return null;

        // Clear input
        input.value = '';

        return text;
    },

    // updates current model name
    setCurrentModelName() {
        var appState = getState();
        $('#current-model-name').innerHTML = appState.currentModel.name;
    },

    renderModelMenu(items, selectedValue) {
        this.modelDropDown.render(items, selectedValue);
        this.setCurrentModelName();
    },
    /**
     * Append a new message to the chat container.
     * @param {string} content - Message text
     * @param {'user' | 'assistant'} role - Message sender
     */
    appendMessage(text, sender) {
        const container = document.getElementById('chat-container');
        const element = document.getElementById('response-loading');

        if (!container || !element) return null;

        const parent = element.parentElement;

        parent.removeChild(element);

        const messageDiv = messageView.createMessageElement(text, sender);

        container.appendChild(messageDiv);

        parent.appendChild(element);

        // Auto-scroll to bottom
        container.scrollTop = container.scrollHeight;

        return messageDiv;
    },

    appendErrorMessage(text) {
        const container = document.getElementById('chat-container');
        const element = document.getElementById('response-loading');
        const parent = element.parentElement;

        parent.removeChild(element);

        const errorBox = $('#error-box').cloneNode(true);

        errorBox.classList.remove('hidden');

        const messageEl = errorBox.querySelector('.codify-error-box__message');

        messageEl.textContent = text;

        container.appendChild(errorBox);

        parent.appendChild(element);
        // Auto-scroll to bottom
        container.scrollTop = container.scrollHeight;
    },

    handleSendMessage(input) {
        togglePanelHidden('#chat-welcome', false);
        this.handleSend(this.getInputMessage(input));
        input.style.height = (this.inputMinHeight) + 'px';
    },
    renderMessages(messages) {
        togglePanelHidden('#chat-welcome', false);
        for (const message of messages) {
            this.appendMessage(message.content, message.role);
        }
    },
    /**
     * Updates an existing streaming message by appending new text.
     * @param {HTMLElement} contentEl - The message content element returned from createStreamingMessage
     * @param {string} chunk - The streamed text chunk
     */
    updateMessage(contentEl, chunk) {

        if (!contentEl) return;

        contentEl.innerHTML += chunk;

        scrollToBottom();
    },

    /**
     * Finalizes a streaming message once the full AI response is received.
     * This can be used to perform final formatting (markdown, code highlighting, etc).
     * @param {HTMLElement} contentEl
     * @param {string} finalText
     */
    finalizeMessage(contentEl, finalText) {

        if (!contentEl) return;

        // Render markdown/code only once after the stream is complete.
        contentEl.innerHTML = CodeRenderer.render(finalText || contentEl.textContent);

        scrollToBottom();
    },
}

/**
 * Creates an empty assistant message element for streaming.
 * Returns the content element so it can be updated progressively.
 */
export function createStreamingMessage() {

    const container = document.getElementById('chat-container');

    const element = document.getElementById('response-loading');
    const parent = element.parentElement;

    parent.removeChild(element);

    const contentEl = messageView.createStreamingMessage();

    parent.appendChild(element);

    scrollToBottom();

    return contentEl;
}



/**
 * Scroll chat to bottom smoothly
 */
export function scrollToBottom() {

    const container = document.getElementById('chat-container');

    if (!container) return;

    container.scrollTo({
        top: container.scrollHeight,
        behavior: 'smooth'
    });
}
