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
import { FloatingDateSeparatorView, defaultChatDateSeparatorFormatter } from './floatingDateSeparatorView.js';
import { ThoughtGroupView } from './thoughtGroupView.js';

const ISO_DATE_TIME_WITHOUT_TIMEZONE = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?$/;
const EXPLICIT_TIMEZONE = /(?:z|[+-]\d{2}:?\d{2})$/i;

function parseChatDate(value) {
    if (!value) return null;

    if (value instanceof Date) {
        return isNaN(value.getTime()) ? null : value;
    }

    if (typeof value === 'string') {
        const trimmedValue = value.trim();
        const normalizedValue = ISO_DATE_TIME_WITHOUT_TIMEZONE.test(trimmedValue) && !EXPLICIT_TIMEZONE.test(trimmedValue)
            ? `${trimmedValue}Z`
            : trimmedValue;
        const date = new Date(normalizedValue);

        return isNaN(date.getTime()) ? null : date;
    }

    const date = new Date(value);

    return isNaN(date.getTime()) ? null : date;
}

export const chatView = {

    normalizeCapabilityProbeResult(value) {
        if (value === true) return 'supported';
        if (value === false) return 'unsupported';

        if (typeof value === 'number') {
            if (value === 0) return 'supported';
            if (value === 1) return 'unsupported';

            return 'unknown';
        }

        if (typeof value === 'string') {
            const normalized = value.toLowerCase();

            if (normalized === 'supported') return 'supported';
            if (normalized === 'unsupported') return 'unsupported';
        }

        return 'unknown';
    },

    getModelCapability(item, camelCaseName, pascalCaseName) {
        return item?.[camelCaseName] ?? item?.[pascalCaseName];
    },

    renderModelCapabilityIcon(item, camelCaseName, pascalCaseName, iconName, label) {
        const state = this.normalizeCapabilityProbeResult(
            this.getModelCapability(item, camelCaseName, pascalCaseName)
        );

        if (state === 'unknown') return '';

        const titleState = state === 'supported' ? 'Supported' : 'Not supported';
        const tooltip = `${label}: ${titleState}`;

        return `<span class="model-capability-tooltip" data-tooltip="${tooltip}" aria-label="${tooltip}"><codinex-icon name="${iconName}" class="model-capability-icon ${state}"></codinex-icon></span>`;
    },

    renderModelCapabilityIcons(item) {
        const icons = [
            this.renderModelCapabilityIcon(item, 'supportsStreaming', 'SupportsStreaming', 'lightning', 'Streaming'),
            this.renderModelCapabilityIcon(item, 'supportsToolCalling', 'SupportsToolCalling', 'wrench', 'Tool calling'),
            this.renderModelCapabilityIcon(item, 'supportsVision', 'SupportsVision', 'eye', 'Vision'),
            this.renderModelCapabilityIcon(item, 'supportsReasoning', 'SupportsReasoning', 'brain', 'Reasoning'),
        ].filter(Boolean);

        if (icons.length === 0) return '';

        return `<div class="model-capabilities">${icons.join('')}</div>`;
    },

    initialize(handleSend, onModelSelected, handleCancel) {
        this.handleSend = handleSend;
        this.handleCancel = handleCancel;

        this.initializeModelDropdown(onModelSelected);
        this.initializeFloatingDateSeparator();

        this.composer = new ComposerView({
            onSend: () => this.handleSendMessage(),
            onCancel: () => this.handleCancelGeneration(),
        });

        this.bindLoadingState();
    },

    initializeFloatingDateSeparator() {
        this.floatingDateSeparator = new FloatingDateSeparatorView({
            containerId: 'chat-container',
            headerId: 'floating-date-separator',
            formatter: defaultChatDateSeparatorFormatter
        });

        this.floatingDateSeparator.initialize();
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
                        <codinex-icon name="lightning" class="low-vis"></codinex-icon>
                        <span>${item.name}</span>
                    </div>
                    ${this.renderModelCapabilityIcons(item)}`;

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

            if (this.composer) {
                this.composer.setStreaming(state.isLoading);
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

    setModelDropdownProviderName() {
        const providerNameElement = $('#model-provider-name');
        const appState = getState();
        const providerName = appState.provider?.name || appState.provider?.Name || '';

        if (!providerNameElement) return;

        providerNameElement.textContent = providerName;
        togglePanelHidden('#model-provider-name', !!providerName);
    },

    renderModelMenu(items, selectedValue) {
        if (!this.modelDropDown) return;

        this.modelDropDown.render(items || [], selectedValue);
        this.setCurrentModelName();
        this.setModelDropdownProviderName();
    },

    dispatchComposerEvent(name, detail) {
        document.dispatchEvent(new CustomEvent(name, { detail }));
    },

    setStatus(message) {
        const statusElement = $('#response-status');

        if (!statusElement) return;

        statusElement.textContent = message || '';
        togglePanelHidden('#response-status', !!message);
        scrollToBottom();
    },

    clearStatus() {
        this.setStatus('');
    },

    /**
     * Creates a brand-new thinking box for this turn and appends it to the
     * timeline right above where the assistant's answer is about to land.
     * Previous turns' boxes are left untouched so they keep their place in order.
     */
    showThinking() {
        const template = $('#thinking-box-template');
        const container = $('#chat-container');
        const statusElement = $('#response-status');

        if (!template || !container) return;

        const box = template.cloneNode(true);

        box.removeAttribute('id');
        box.classList.remove('hidden');
        // Closed by default — the user opts in to see the reasoning.
        box.classList.add('collapsed');
        box.classList.add('streaming');

        const label = box.querySelector('.thinking-label');
        const toggleBtn = box.querySelector('.thinking-toggle');

        if (toggleBtn) {
            toggleBtn.setAttribute('aria-expanded', 'false');
            toggleBtn.addEventListener('click', () => {
                const collapsed = box.classList.toggle('collapsed');

                toggleBtn.setAttribute('aria-expanded', String(!collapsed));
            });
        }

        if (statusElement) {
            container.insertBefore(box, statusElement);
        } else {
            container.appendChild(box);
        }

        const thinkingContent = box.querySelector('.thinking-content');

        this.currentThinkingBox = box;
        this.currentThoughtGroup = thinkingContent ? new ThoughtGroupView(thinkingContent) : null;
        this.thinkingStartedAt = Date.now();
        this.stopThinkingTimer();

        this.thinkingTimerId = setInterval(() => {
            if (!label || !this.thinkingStartedAt) return;

            const duration = this.formatThinkingDuration(Date.now() - this.thinkingStartedAt);

            label.textContent = `Thinking... ${duration}`;
        }, 1000);

        scrollToBottom();
    },

    appendThinking(chunk) {
        if (!this.currentThoughtGroup || !chunk) return;

        this.currentThoughtGroup.appendChunk(chunk);

        scrollToBottom();
    },

    stopThinkingTimer() {
        if (this.thinkingTimerId) {
            clearInterval(this.thinkingTimerId);
            this.thinkingTimerId = null;
        }
    },

    formatThinkingDuration(ms) {
        const totalSeconds = Math.max(0, Math.round(ms / 1000));
        const minutes = Math.floor(totalSeconds / 60);
        const seconds = totalSeconds % 60;

        return minutes > 0 ? `${minutes}m ${seconds}s` : `${seconds}s`;
    },

    completeThinking() {
        const box = this.currentThinkingBox;

        // Already finalized this turn (e.g. real ThinkingCompleted event already
        // ran) — don't let the defensive call from handleAIResponse/handleAIError
        // clobber the duration label.
        if (!box || !box.classList.contains('streaming')) return;

        this.stopThinkingTimer();
        box.classList.remove('streaming');

        const label = box.querySelector('.thinking-label');

        if (label) {
            const duration = this.thinkingStartedAt
                ? this.formatThinkingDuration(Date.now() - this.thinkingStartedAt)
                : null;

            label.textContent = duration ? `Thought for ${duration}` : 'Thought process';
        }

        this.thinkingStartedAt = null;
        this.currentThinkingBox = null;
        this.currentThoughtGroup = null;
    },

    resetThinking() {
        this.stopThinkingTimer();
        this.thinkingStartedAt = null;
        this.currentThinkingBox = null;
        this.currentThoughtGroup = null;
    },

    getMessageDate(message) {
        const value = message?.createdAt || message?.CreatedAt || message?.timestamp || message?.Timestamp;
        const date = parseChatDate(value);

        return date || new Date();
    },

    tagMessageElement(element, date, deferRefresh) {
        if (!element) return;

        const messageDate = parseChatDate(date) || new Date();

        element.dataset.messageCreatedAt = messageDate.toISOString();

        if (!deferRefresh && this.floatingDateSeparator) {
            this.floatingDateSeparator.refresh();
        }
    },

    /**
     * Append a new message to the chat container.
     * @param {string} content - Message text
     * @param {'user' | 'assistant'} role - Message sender
     */
    appendMessage(text, sender, createdAt, deferDateSeparatorRefresh, options) {
        const container = document.getElementById('chat-container');
        const element = document.getElementById('response-loading');
        const statusElement = document.getElementById('response-status');

        if (!container || !element) return null;

        const parent = element.parentElement;

        if (statusElement) parent.removeChild(statusElement);
        parent.removeChild(element);

        const messageOptions = sender === 'user'
            ? { ...options, messageIndex: container.querySelectorAll('.chat-message').length }
            : options;

        const messageDiv = messageView.createMessageElement(text, sender, messageOptions);

        this.tagMessageElement(messageDiv, createdAt || new Date(), deferDateSeparatorRefresh);

        // The current turn's thinking box (if any) was already placed right
        // before this insertion point by showThinking(), so it naturally ends
        // up directly above this message.
        container.appendChild(messageDiv);

        if (statusElement) parent.appendChild(statusElement);
        parent.appendChild(element);

        // Auto-scroll to bottom
        container.scrollTop = container.scrollHeight;

        return messageDiv;
    },

    appendErrorMessage(text) {
        const container = document.getElementById('chat-container');
        const element = document.getElementById('response-loading');
        const statusElement = document.getElementById('response-status');
        const parent = element.parentElement;

        if (statusElement) parent.removeChild(statusElement);
        parent.removeChild(element);

        const errorBox = $('#error-box').cloneNode(true);

        errorBox.classList.remove('hidden');

        const messageEl = errorBox.querySelector('.codinex-error-box__message');

        messageEl.textContent = text;

        container.appendChild(errorBox);

        if (statusElement) parent.appendChild(statusElement);
        parent.appendChild(element);
        // Auto-scroll to bottom
        container.scrollTop = container.scrollHeight;
    },

    handleSendMessage() {
        togglePanelHidden('#chat-welcome', false);
        this.handleSend();
    },
    handleCancelGeneration() {
        if (this.handleCancel) {
            this.handleCancel();
        }
    },
    renderMessages(messages) {
        const chatMessages = messages || [];

        togglePanelHidden('#chat-welcome', chatMessages.length === 0);

        for (const message of chatMessages) {
            const context = message.context || message.Context;

            this.appendMessage(message.content, message.role, this.getMessageDate(message), true, context ? { context } : undefined);
        }

        if (this.floatingDateSeparator) {
            this.floatingDateSeparator.refresh();
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
        CodeRenderer.bindCopyEvents(contentEl);

        scrollToBottom();
    },

    clearMessages() {
        const container = document.getElementById('chat-container');

        if (!container) return;

        const statusElement = document.getElementById('response-status');
        const loadingElement = document.getElementById('response-loading');
        const errorBox = document.getElementById('error-box');
        const thinkingBoxTemplate = document.getElementById('thinking-box-template');

        this.stopThinkingTimer();
        this.thinkingStartedAt = null;
        this.currentThinkingBox = null;
        this.currentThoughtGroup = null;

        container.textContent = '';

        if (thinkingBoxTemplate) {
            thinkingBoxTemplate.classList.add('hidden');
            thinkingBoxTemplate.classList.remove('collapsed', 'streaming');

            const thinkingContent = thinkingBoxTemplate.querySelector('.thinking-content');
            const thinkingLabel = thinkingBoxTemplate.querySelector('.thinking-label');

            if (thinkingContent) thinkingContent.innerHTML = '';
            if (thinkingLabel) thinkingLabel.textContent = 'Thinking...';

            container.appendChild(thinkingBoxTemplate);
        }

        if (statusElement) {
            statusElement.textContent = '';
            statusElement.classList.add('hidden');
            container.appendChild(statusElement);
        }

        if (loadingElement) {
            loadingElement.classList.add('hidden');
            container.appendChild(loadingElement);
        }

        if (errorBox) {
            errorBox.classList.add('hidden');
            container.appendChild(errorBox);
        }

        togglePanelHidden('#chat-welcome', true);

        if (this.floatingDateSeparator) {
            this.floatingDateSeparator.refresh();
        }
    }
}

/**
 * Creates an empty assistant message element for streaming.
 * Returns the content element so it can be updated progressively.
 */
export function createStreamingMessage(options) {

    const container = document.getElementById('chat-container');

    const element = document.getElementById('response-loading');
    const statusElement = document.getElementById('response-status');
    const parent = element.parentElement;

    if (statusElement) parent.removeChild(statusElement);
    parent.removeChild(element);

    const contentEl = messageView.createStreamingMessage(options);

    chatView.tagMessageElement(contentEl.parentElement, new Date());

    if (statusElement) parent.appendChild(statusElement);
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
