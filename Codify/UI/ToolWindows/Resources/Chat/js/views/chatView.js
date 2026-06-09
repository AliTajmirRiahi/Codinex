/**
 * ChatView
 * Responsible only for rendering UI elements.
 * No business logic or AI communication should exist here.
 */

import { addMessage } from '../state/appState.js';

/**
 * Append a new message to the chat container.
 * @param {string} content - Message text
 * @param {'user' | 'assistant'} role - Message sender
 */
export function appendMessage(content, role) {

    const container = document.getElementById('chat-container');

    if (!container) {
        console.warn('Chat container not found');
        return;
    }

    // Create message wrapper
    const messageEl = document.createElement('div');
    messageEl.classList.add('chat-message', role);

    // Create content element
    const contentEl = document.createElement('div');
    contentEl.classList.add('message-content');

    contentEl.innerText = content;

    messageEl.appendChild(contentEl);
    container.appendChild(messageEl);

    // Save message in state
    addMessage({ role, content });

    scrollToBottom();
}


/**
 * Creates an empty assistant message element for streaming.
 * Returns the content element so it can be updated progressively.
 */
export function createStreamingMessage() {

    const container = document.getElementById('chat-container');

    const messageEl = document.createElement('div');
    messageEl.classList.add('chat-message', 'assistant');

    const contentEl = document.createElement('div');
    contentEl.classList.add('message-content');

    messageEl.appendChild(contentEl);
    container.appendChild(messageEl);

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
