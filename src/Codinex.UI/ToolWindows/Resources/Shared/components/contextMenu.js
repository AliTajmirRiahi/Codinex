/**
 * contextMenu.js
 * Replaces the WebView2/Chromium default right-click menu everywhere in the
 * extension with a themed menu that only exposes the actions that make sense
 * for the element under the cursor:
 *   - Editable composer inputs (#userInput / #contextInput): Cut, Copy, Paste, Select All
 *   - Chat messages: Select Message, Copy, Cut (of the current text selection)
 * Right-clicking anywhere else simply suppresses the native menu.
 */

const EDITABLE_SELECTOR = '#userInput, #contextInput, [contenteditable="true"]';
const MESSAGE_SELECTOR = '.chat-message';

let menuEl = null;
let savedRange = null;

function isSelectionWithin(el) {
    const selection = window.getSelection();

    if (!selection || selection.rangeCount === 0 || selection.isCollapsed) return false;

    const range = selection.getRangeAt(0);

    return el.contains(range.commonAncestorContainer);
}

function getCaretRangeAtPoint(x, y) {
    if (document.caretRangeFromPoint) {
        return document.caretRangeFromPoint(x, y);
    }

    return null;
}

async function copyText(text) {
    if (!text) return;

    try {
        await navigator.clipboard.writeText(text);
    } catch {
        // Clipboard access denied/unavailable — silently ignore, nothing else we can do.
    }
}

function restoreSelection(target) {
    if (!savedRange) return false;

    target.focus();

    const selection = window.getSelection();

    selection.removeAllRanges();
    selection.addRange(savedRange);

    return true;
}

function buildEditableItems(target) {
    const hasSelection = isSelectionWithin(target);

    return [
        {
            label: 'Cut',
            disabled: !hasSelection,
            action: () => {
                if (!restoreSelection(target)) return;
                document.execCommand('cut');
            }
        },
        {
            label: 'Copy',
            disabled: !hasSelection,
            action: () => {
                if (!restoreSelection(target)) return;
                document.execCommand('copy');
            }
        },
        {
            label: 'Paste',
            action: async () => {
                target.focus();

                if (savedRange) {
                    const selection = window.getSelection();
                    selection.removeAllRanges();
                    selection.addRange(savedRange);
                }

                try {
                    const text = await navigator.clipboard.readText();
                    if (text) document.execCommand('insertText', false, text);
                } catch {
                    // Clipboard read denied/unavailable — nothing else we can do.
                }
            }
        },
        { separator: true },
        {
            label: 'Select All',
            action: () => {
                target.focus();

                const range = document.createRange();
                range.selectNodeContents(target);

                const selection = window.getSelection();
                selection.removeAllRanges();
                selection.addRange(range);
            }
        }
    ];
}

function buildMessageItems(messageEl) {
    const contentEl = messageEl.querySelector('.message-content') || messageEl;
    const hasSelection = isSelectionWithin(contentEl);

    return [
        {
            label: 'Select Message',
            action: () => {
                const range = document.createRange();
                range.selectNodeContents(contentEl);

                const selection = window.getSelection();
                selection.removeAllRanges();
                selection.addRange(range);
            }
        },
        {
            label: 'Copy',
            action: () => {
                const selection = window.getSelection();
                const text = hasSelection ? selection.toString() : contentEl.innerText;

                copyText(text);
            }
        },
        {
            label: 'Cut',
            disabled: !hasSelection,
            action: () => {
                const selection = window.getSelection();
                const text = selection.toString();

                copyText(text);

                // Messages are read-only/persisted — "cut" only removes the
                // selected text from the on-screen rendering, not the stored chat.
                selection.getRangeAt(0).deleteContents();
                selection.removeAllRanges();
            }
        }
    ];
}

function ensureMenuElement() {
    if (menuEl) return menuEl;

    menuEl = document.createElement('div');
    menuEl.className = 'codinex-context-menu hidden';
    document.body.appendChild(menuEl);

    return menuEl;
}

function hideMenu() {
    if (!menuEl) return;

    menuEl.classList.add('hidden');
    menuEl.innerHTML = '';
}

function showMenu(x, y, items) {
    const menu = ensureMenuElement();

    menu.innerHTML = '';

    items.forEach((item) => {
        if (item.separator) {
            const separator = document.createElement('div');
            separator.className = 'codinex-context-menu__separator';
            menu.appendChild(separator);
            return;
        }

        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'codinex-context-menu__item';
        button.textContent = item.label;
        button.disabled = !!item.disabled;

        button.addEventListener('click', (e) => {
            e.stopPropagation();
            hideMenu();
            if (!item.disabled) item.action();
        });

        menu.appendChild(button);
    });

    menu.classList.remove('hidden');

    // Position after render so we can clamp to the viewport using real dimensions.
    const rect = menu.getBoundingClientRect();
    const maxX = window.innerWidth - rect.width - 4;
    const maxY = window.innerHeight - rect.height - 4;

    menu.style.left = `${Math.max(4, Math.min(x, maxX))}px`;
    menu.style.top = `${Math.max(4, Math.min(y, maxY))}px`;
}

function handleContextMenu(e) {
    e.preventDefault();

    const editable = e.target.closest(EDITABLE_SELECTOR);
    const message = !editable ? e.target.closest(MESSAGE_SELECTOR) : null;

    if (!editable && !message) {
        hideMenu();
        return;
    }

    const selection = window.getSelection();
    savedRange = selection && selection.rangeCount > 0 ? selection.getRangeAt(0).cloneRange() : null;

    if (editable && !isSelectionWithin(editable)) {
        // No active selection: place the caret where the user right-clicked so Paste lands there.
        const caretRange = getCaretRangeAtPoint(e.clientX, e.clientY);
        if (caretRange) savedRange = caretRange;
    }

    const items = editable ? buildEditableItems(editable) : buildMessageItems(message);

    showMenu(e.clientX, e.clientY, items);
}

export function initContextMenu() {
    document.addEventListener('contextmenu', handleContextMenu);

    document.addEventListener('mousedown', (e) => {
        if (menuEl && !menuEl.contains(e.target)) hideMenu();
    });

    document.addEventListener('scroll', hideMenu, true);
    window.addEventListener('blur', hideMenu);

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') hideMenu();
    });
}
