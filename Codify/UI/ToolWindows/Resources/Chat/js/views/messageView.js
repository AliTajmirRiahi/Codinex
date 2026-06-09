import { createElement } from '../utils/dom.js';

/**
 * Specifically handles message rendering logic.
 */
export const messageView = {
    createMessageElement(role, content) {
        const container = createElement('div', `message-item ${role}`);
        const avatar = createElement('div', 'avatar', `<codify-icon name="${role}-avatar"></codify-icon>`);
        const text = createElement('div', 'content', content);

        container.appendChild(avatar);
        container.appendChild(text);
        return container;
    }
};
