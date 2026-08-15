/**
 * selectionToolbar.js
 * A small floating toolbar that appears automatically above a text
 * selection made inside an assistant chat message, offering:
 *   - Ask Codinex: quotes the selected text into the composer
 *   - Copy: copies the selected text to the clipboard
 * It never appears for user messages or for selections outside a message.
 */
import { CodeRenderer } from './code-renderer.js';

const MESSAGE_CONTENT_SELECTOR = '.chat-message.assistant .message-content';

let toolbarEl = null;
let currentText = '';

function isSelectionWithinAssistantMessage() {
    const selection = window.getSelection();

    if (!selection || selection.rangeCount === 0 || selection.isCollapsed) return null;

    const range = selection.getRangeAt(0);
    const container = range.commonAncestorContainer;
    const el = container.nodeType === Node.ELEMENT_NODE ? container : container.parentElement;
    const contentEl = el ? el.closest(MESSAGE_CONTENT_SELECTOR) : null;

    if (!contentEl) return null;

    const text = selection.toString();

    if (!text.trim()) return null;

    return { range, text };
}

async function copyText(text) {
    if (!text) return;

    try {
        await CodeRenderer.copyToClipboard(text);
    } catch {
        // Clipboard access denied/unavailable — silently ignore, nothing else we can do.
    }
}

function ensureToolbarElement() {
    if (toolbarEl) return toolbarEl;

    toolbarEl = document.createElement('div');
    toolbarEl.className = 'codinex-selection-toolbar hidden';

    const askBtn = document.createElement('button');
    askBtn.type = 'button';
    askBtn.className = 'codinex-selection-toolbar__item';
    askBtn.textContent = 'Ask Codinex';
    askBtn.addEventListener('mousedown', (e) => e.preventDefault());
    askBtn.addEventListener('click', (e) => {
        e.stopPropagation();

        document.dispatchEvent(new CustomEvent('composer:quote-message', {
            detail: { text: currentText }
        }));

        hideToolbar();
    });

    const separator = document.createElement('div');
    separator.className = 'codinex-selection-toolbar__separator';

    const copyBtn = document.createElement('button');
    copyBtn.type = 'button';
    copyBtn.className = 'codinex-selection-toolbar__item';
    copyBtn.textContent = 'Copy';
    copyBtn.addEventListener('mousedown', (e) => e.preventDefault());
    copyBtn.addEventListener('click', (e) => {
        e.stopPropagation();
        copyText(currentText);
        hideToolbar();
    });

    toolbarEl.appendChild(askBtn);
    toolbarEl.appendChild(separator);
    toolbarEl.appendChild(copyBtn);
    document.body.appendChild(toolbarEl);

    return toolbarEl;
}

function hideToolbar() {
    if (!toolbarEl) return;

    toolbarEl.classList.add('hidden');
    currentText = '';
}

function showToolbar(range, text) {
    const toolbar = ensureToolbarElement();

    currentText = text;
    toolbar.classList.remove('hidden');

    const rangeRect = range.getBoundingClientRect();
    const toolbarRect = toolbar.getBoundingClientRect();

    const centeredX = rangeRect.left + (rangeRect.width / 2) - (toolbarRect.width / 2);
    const maxX = window.innerWidth - toolbarRect.width - 4;
    const x = Math.max(4, Math.min(centeredX, maxX));

    // Prefer above the selection; flip below when there isn't enough room.
    const spaceAbove = rangeRect.top;
    const showBelow = spaceAbove < toolbarRect.height + 8;
    const y = showBelow
        ? Math.min(window.innerHeight - toolbarRect.height - 4, rangeRect.bottom + 8)
        : rangeRect.top - toolbarRect.height - 8;

    toolbar.style.left = `${x}px`;
    toolbar.style.top = `${Math.max(4, y)}px`;
}

function handleSelectionChange() {
    const match = isSelectionWithinAssistantMessage();

    if (!match) {
        hideToolbar();
        return;
    }

    showToolbar(match.range, match.text);
}

export function initSelectionToolbar() {
    // 'selectionchange' fires continuously while dragging; only react once the
    // mouse/keyboard interaction that produced the selection has settled.
    document.addEventListener('mouseup', () => setTimeout(handleSelectionChange, 0));
    document.addEventListener('keyup', (e) => {
        if (e.shiftKey || e.key === 'Shift') setTimeout(handleSelectionChange, 0);
    });

    document.addEventListener('mousedown', (e) => {
        if (toolbarEl && !toolbarEl.contains(e.target)) hideToolbar();
    });

    document.addEventListener('scroll', hideToolbar, true);
    window.addEventListener('blur', hideToolbar);
    window.addEventListener('resize', hideToolbar);

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') hideToolbar();
    });
}
