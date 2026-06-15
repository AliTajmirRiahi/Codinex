import { createElement } from '../utils/dom.js';
import { CodeRenderer } from "../../../Shared/components/code-renderer.js";

/**
 * Specifically handles message rendering logic.
 */
export const messageView = {
    createMessageElement(text, sender) {
        const messageDiv = document.createElement('div');

        // Add base and sender-specific classes
        messageDiv.className = `chat-message ${sender}`;

        // Optional: add data-sender for the CSS label
        messageDiv.setAttribute('data-sender', sender === 'user' ? 'You' : 'Codify AI');

        messageDiv.innerHTML = `<div class="message-content">${CodeRenderer.render(text)}</div>`;

        return messageDiv;
    }
};
