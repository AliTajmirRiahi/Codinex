import { CodeRenderer } from "../../../Shared/components/code-renderer.js";
import { getState } from "../state/appState.js";

function getCurrentModelName() {
    const state = getState();

    return state.currentChat?.modelId
        || state.currentModel?.name
        || state.currentModel?.id
        || '';
}

function createMessageHeader(sender) {
    const headerEl = document.createElement('div');
    headerEl.className = 'message-header';

    if (sender === 'assistant') {
        const logoEl = document.createElement('codify-image');
        logoEl.setAttribute('name', 'codify-AI-logo-black.svg');
        logoEl.setAttribute('alt', 'Codify AI Logo');
        logoEl.className = 'message-header-logo';

        const titleEl = document.createElement('span');
        titleEl.textContent = 'CODIFY AI';

        headerEl.appendChild(logoEl);
        headerEl.appendChild(titleEl);
    } else {
        headerEl.textContent = 'You';
    }

    return headerEl;
}

function createModelFooter() {
    const modelName = getCurrentModelName();

    if (!modelName) return null;

    const footerEl = document.createElement('div');
    footerEl.className = 'message-model-name';
    footerEl.textContent = modelName;

    return footerEl;
}

/**
 * Specifically handles message rendering logic.
 */
export const messageView = {
    createMessageElement(text, sender) {
        const messageDiv = document.createElement('div');

        // Add base and sender-specific classes
        messageDiv.className = `chat-message ${sender}`;

        const contentEl = document.createElement('div');
        contentEl.className = 'message-content';
        contentEl.innerHTML = CodeRenderer.render(text);
        CodeRenderer.bindCopyEvents(contentEl);

        messageDiv.appendChild(createMessageHeader(sender));
        messageDiv.appendChild(contentEl);

        if (sender === 'assistant') {
            const footerEl = createModelFooter();

            if (footerEl) {
                messageDiv.appendChild(footerEl);
            }
        }

        return messageDiv;
    },
    createStreamingMessage() {
        const messageDiv = document.createElement('div');
        messageDiv.className = 'chat-message assistant';

        const contentEl = document.createElement('div');
        contentEl.className = 'message-content';

        messageDiv.appendChild(createMessageHeader('assistant'));
        messageDiv.appendChild(contentEl);

        const footerEl = createModelFooter();

        if (footerEl) {
            messageDiv.appendChild(footerEl);
        }

        document.getElementById('chat-container').appendChild(messageDiv);

        return contentEl;
    }
};
