import { CodeRenderer } from "../../../Shared/components/code-renderer.js";
import { getState } from "../state/appState.js";

function isPreprocessorAnswer(options) {
    return !!(options && (options.isPreprocessorAnswer || options.IsPreprocessorAnswer));
}

function getPreprocessorModelName(state) {
    return state.settings?.preprocessorAiModelId
        || state.settings?.PreprocessorAiModelId
        || '';
}

function getCurrentModelName(options) {
    const state = getState();

    if (isPreprocessorAnswer(options)) {
        const modelName = getPreprocessorModelName(state);

        return modelName
            ? `preprocessing answer : ${modelName}`
            : 'preprocessing answer';
    }

    return state.currentChat?.modelId
        || state.currentModel?.name
        || state.currentModel?.id
        || '';
}

function createMessageHeader(sender) {
    const headerEl = document.createElement('div');
    headerEl.className = 'message-header';

    if (sender === 'assistant') {
        const logoEl = document.createElement('codinex-image');
        logoEl.setAttribute('name', 'codinex-AI-logo-black.svg');
        logoEl.setAttribute('alt', 'Codinex AI Logo');
        logoEl.className = 'message-header-logo';

        const titleEl = document.createElement('span');
        titleEl.textContent = 'CODINEX AI';

        headerEl.appendChild(logoEl);
        headerEl.appendChild(titleEl);
    } else {
        headerEl.textContent = 'You';
    }

    return headerEl;
}

function createModelFooter(options) {
    const modelName = getCurrentModelName(options);

    if (!modelName) return null;

    const footerEl = document.createElement('div');
    footerEl.className = 'message-model-name';
    footerEl.textContent = modelName;

    return footerEl;
}

function createUserMessageCopyButton(text) {
    const buttonEl = document.createElement('button');
    buttonEl.type = 'button';
    buttonEl.className = 'message-copy-btn';
    buttonEl.title = 'Copy message';
    buttonEl.setAttribute('aria-label', 'Copy message');
    buttonEl.innerHTML = '<codinex-icon name="copy" aria-hidden="true"></codinex-icon>';

    buttonEl.addEventListener('click', async () => {
        const originalTitle = buttonEl.title;
        const originalLabel = buttonEl.getAttribute('aria-label');

        try {
            await CodeRenderer.copyToClipboard(text || '');

            buttonEl.title = 'Copied';
            buttonEl.setAttribute('aria-label', 'Copied');
            buttonEl.classList.add('copied');
        } catch {
            buttonEl.title = 'Copy failed';
            buttonEl.setAttribute('aria-label', 'Copy failed');
        }

        setTimeout(() => {
            buttonEl.title = originalTitle;

            if (originalLabel) {
                buttonEl.setAttribute('aria-label', originalLabel);
            } else {
                buttonEl.removeAttribute('aria-label');
            }

            buttonEl.classList.remove('copied');
        }, 1500);
    });

    return buttonEl;
}

function createRewindToHereButton(messageIndex) {
    const buttonEl = document.createElement('button');
    buttonEl.type = 'button';
    buttonEl.className = 'message-rewind-btn';
    buttonEl.title = 'Rewind to here';
    buttonEl.setAttribute('aria-label', 'Rewind to here');
    buttonEl.innerHTML = '<codinex-icon name="refresh-cw" aria-hidden="true"></codinex-icon><span>Rewind to here</span>';

    buttonEl.addEventListener('click', () => {
        document.dispatchEvent(new CustomEvent('chat:rewind-to-message', {
            detail: { messageIndex }
        }));
    });

    return buttonEl;
}

function createUserMessageActions(text, messageIndex) {
    const actionsEl = document.createElement('div');
    actionsEl.className = 'message-actions';

    actionsEl.appendChild(createUserMessageCopyButton(text));

    if (Number.isInteger(messageIndex)) {
        actionsEl.appendChild(createRewindToHereButton(messageIndex));
    }

    return actionsEl;
}

function createUserMessageElement(messageDiv, text, messageIndex) {
    const messageGroupEl = document.createElement('div');
    messageGroupEl.className = 'chat-message-group user-message-group';

    messageGroupEl.appendChild(messageDiv);
    messageGroupEl.appendChild(createUserMessageActions(text, messageIndex));

    return messageGroupEl;
}

/**
 * Specifically handles message rendering logic.
 */
export const messageView = {
    createMessageElement(text, sender, options) {
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
            const footerEl = createModelFooter(options);

            if (footerEl) {
                messageDiv.appendChild(footerEl);
            }
        }

        if (sender === 'user') {
            return createUserMessageElement(messageDiv, text, options?.messageIndex);
        }

        return messageDiv;
    },
    createStreamingMessage(options) {
        const messageDiv = document.createElement('div');
        messageDiv.className = 'chat-message assistant';

        const contentEl = document.createElement('div');
        contentEl.className = 'message-content';

        messageDiv.appendChild(createMessageHeader('assistant'));
        messageDiv.appendChild(contentEl);

        const footerEl = createModelFooter(options);

        if (footerEl) {
            messageDiv.appendChild(footerEl);
        }

        document.getElementById('chat-container').appendChild(messageDiv);

        return contentEl;
    }
};
