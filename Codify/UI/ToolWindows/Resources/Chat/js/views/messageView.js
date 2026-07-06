import { createElement } from '../utils/dom.js';
import { CodeRenderer } from "../../../Shared/components/code-renderer.js";
import { getState } from "../state/appState.js"; 

/**
 * Specifically handles message rendering logic.
 */
export const messageView = {
    createMessageElement(text, sender) {
        const messageDiv = document.createElement('div');

        // Add base and sender-specific classes
        messageDiv.className = `chat-message ${sender}`;

        var state = getState();
        // Optional: add data-sender for the CSS label
        messageDiv.setAttribute('data-sender', sender === 'user' ? 'You' : `Codify AI (${state.currentChat.providerId} ${state.currentChat.modelId}) `);

        messageDiv.innerHTML = `<div class="message-content">${CodeRenderer.render(text)}</div>`;

        return messageDiv;
    },
    createStreamingMessage() {
        const messageDiv = document.createElement('div');
        messageDiv.className = 'chat-message assistant';

        const contentEl = document.createElement('div');
        contentEl.className = 'message-content';

        messageDiv.appendChild(contentEl);

        document.getElementById('chat-container').appendChild(messageDiv);

        return contentEl;
    }
};
