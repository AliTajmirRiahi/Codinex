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
    buttonEl.innerHTML = '<codinex-icon name="refresh-cw" aria-hidden="true"></codinex-icon>';

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

    if (Number.isInteger(messageIndex)) {
        actionsEl.appendChild(createRewindToHereButton(messageIndex));
    }

    actionsEl.appendChild(createUserMessageCopyButton(text));

    return actionsEl;
}

function getMessageReferences(options) {
    const context = options?.context || options?.Context;

    return context?.selectedReferences || context?.SelectedReferences || [];
}

function getMessageQuote(options) {
    const context = options?.context || options?.Context;

    return context?.quotedMessage || context?.QuotedMessage || null;
}

function createQuoteBox(quote) {
    if (!quote || !quote.text) return null;

    const wrapperEl = document.createElement('div');
    wrapperEl.className = 'message-quote';

    wrapperEl.innerHTML = `
        <div class="message-quote-accent"></div>
        <div class="message-quote-body">
            <div class="message-quote-header">
                <span class="message-quote-mark" aria-hidden="true">&rdquo;</span>
                <span class="message-quote-title">Quoted message</span>
            </div>
            <div class="message-quote-text"></div>
            <div class="message-quote-meta">
                <span class="message-quote-author">${quote.author || 'You'}</span>
                <span class="message-quote-dot">·</span>
                <span class="message-quote-time">${quote.time || ''}</span>
            </div>
        </div>
    `;

    wrapperEl.querySelector('.message-quote-text').textContent = quote.text;

    return wrapperEl;
}

function openReference(ref) {
    document.dispatchEvent(new CustomEvent('composer:ref-click', {
        detail: { id: ref.id || ref.Id, reference: ref }
    }));
}

/**
 * Shortens an absolute file path down to a solution-relative path for display,
 * e.g. "P:\Solution\src\Foo.cs" -> "src\Foo.cs". Falls back to the original
 * path when it isn't under the known solution directory.
 */
function toSolutionRelativePath(filePath) {
    if (!filePath) return filePath;

    const solutionDirectory = getState().solutionDirectory;

    if (!solutionDirectory) return filePath;

    const normalizedPath = filePath.replace(/\//g, '\\');
    const normalizedRoot = solutionDirectory.replace(/\//g, '\\').replace(/\\+$/, '');

    if (normalizedPath.toLowerCase().startsWith(normalizedRoot.toLowerCase() + '\\')) {
        return normalizedPath.slice(normalizedRoot.length + 1);
    }

    return filePath;
}

function createReferenceItem(ref) {
    const refName = ref.name || ref.Name || '';
    const refIcon = ref.icon || ref.Icon;
    const refColor = ref.color || ref.Color;
    const metadata = ref.metadata || ref.Metadata || {};
    const filePath = metadata.filePath || metadata.FilePath || ref.value || ref.Value || '';
    const displayPath = toSolutionRelativePath(filePath);

    const itemEl = document.createElement('button');
    itemEl.type = 'button';
    itemEl.className = 'message-reference-item';
    itemEl.title = filePath || refName;

    itemEl.innerHTML = `
        ${refIcon ? `<span class="item-icon" style="${refColor ? `color: var(${refColor});` : ''}"><codinex-icon name="${refIcon}"></codinex-icon></span>` : ''}
        <span class="message-reference-name">${refName}</span>
        ${displayPath ? `<span class="message-reference-path">${displayPath}</span>` : ''}
    `;

    itemEl.addEventListener('click', () => openReference(ref));

    return itemEl;
}

function createReferencesBox(references) {
    if (!references || references.length === 0) return null;

    const wrapperEl = document.createElement('div');
    wrapperEl.className = 'message-references';

    const toggleEl = document.createElement('button');
    toggleEl.type = 'button';
    toggleEl.className = 'message-references-toggle';
    toggleEl.setAttribute('aria-expanded', 'false');
    toggleEl.innerHTML = `
        <codinex-icon name="folder" aria-hidden="true"></codinex-icon>
        <span>${references.length} reference${references.length > 1 ? 's' : ''}</span>
        <codinex-icon name="chevron-down" class="message-references-chevron" aria-hidden="true"></codinex-icon>
    `;

    const listEl = document.createElement('div');
    listEl.className = 'message-references-list hidden';

    references.forEach((ref) => listEl.appendChild(createReferenceItem(ref)));

    toggleEl.addEventListener('click', () => {
        const isExpanded = !listEl.classList.contains('hidden');

        listEl.classList.toggle('hidden');
        toggleEl.classList.toggle('expanded', !isExpanded);
        toggleEl.setAttribute('aria-expanded', String(!isExpanded));
    });

    wrapperEl.appendChild(toggleEl);
    wrapperEl.appendChild(listEl);

    return wrapperEl;
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

        if (sender === 'user') {
            const quoteBox = createQuoteBox(getMessageQuote(options));

            if (quoteBox) {
                messageDiv.appendChild(quoteBox);
            }

            const referencesBox = createReferencesBox(getMessageReferences(options));

            if (referencesBox) {
                messageDiv.appendChild(referencesBox);
            }
        }

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
