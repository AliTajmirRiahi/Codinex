/**
 * ChatView
 * Responsible only for rendering UI elements.
 * No business logic or AI communication should exist here.
 **/


import { $, togglePanelHidden } from '../utils/dom.js';
import { DropDownView } from '../views/dropDownView.js';
import { getState, setCurrentModel, subscribe } from '../state/appState.js';
import { messageView } from '../views/messageView.js';
import { CodeRenderer } from "../../../Shared/components/code-renderer.js";
import { ComposerView } from './composerView.js';

export const chatView = {

    initialize(handleSend, onModelSelected) {
        this.handleSend = handleSend;

        this.initializeModelDropdown(onModelSelected);

        this.composer = new ComposerView({
            onSend: () => this.handleSendMessage(),
        });

        this.bindLoadingState();
    },

    initializeModelDropdown(onModelSelected) {
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
                if (onModelSelected) {
                    onModelSelected(model);
                }

                setCurrentModel(model);
                this.setCurrentModelName();

                return true;
            }
        });
    },

    bindLoadingState() {
        subscribe((state) => {
            togglePanelHidden('#response-loading', state.isLoading);

            if (this.input) {
                this.input.disabled = state.isLoading;
            }

            if (this.sendBtn) {
                this.sendBtn.disabled = state.isLoading || !this.getInputText();
            }
        });
    },

    getInputMessage(input) {
        const text = input.value.trim();

        if (!text) return null;

        input.value = '';
        this.updateSendButtonState('');
        this.hideComposerMenu();

        return text;
    },

    // updates current model name
    setCurrentModelName() {
        const currentModelName = $('#current-model-name');
        const appState = getState();

        if (!currentModelName || !appState.currentModel) return;

        currentModelName.innerHTML = appState.currentModel.name;
    },

    renderModelMenu(items, selectedValue) {
        if (!this.modelDropDown) return;

        this.modelDropDown.render(items || [], selectedValue);
        this.setCurrentModelName();
    },

    dispatchComposerEvent(name, detail) {
        document.dispatchEvent(new CustomEvent(name, { detail }));
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

    handleSendMessage() {
        togglePanelHidden('#chat-welcome', false);
        this.handleSend();
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

    clearMessages() {
        const chatElements = document.getElementsByClassName('chat-message');

        while (chatElements.length > 0) {
            const parent = chatElements[0].parentElement;
            parent.removeChild(chatElements[0]);
        }
    }
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
