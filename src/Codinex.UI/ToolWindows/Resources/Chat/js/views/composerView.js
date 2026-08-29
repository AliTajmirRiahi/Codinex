/**
 * ComposerView
 * Handles message input, triggers, chips and command menus.
 * No AI logic should exist here.
 */

import { $, togglePanelDisable, togglePanelHidden } from '../utils/dom.js';

const TRIGGERS = {
    '/': 'commands',
    '@': 'agents',
    '#': 'references',
    '+': 'contexts'
};

export class ComposerView {

    constructor({ onSend, onChange, onCancel }) {

        this.onSend = onSend;
        this.onChange = onChange;
        this.onCancel = onCancel;
        this.isStreaming = false;

        this.inputContainer = $('#input-container');
        this.input = $('#userInput');
        this.sendBtn = $('#send-btn');
        this.chipsContainer = $('#composer-chips') || document.querySelector('.composer-chips');
        this.menu = $('#composer-menu') || document.querySelector('.composer-menu');
        this.contextBtn = $('#contextBtn') || document.querySelector('#contextBtn');
        this.contextInput = $('#contextInput') || document.querySelector('#contextInput');

        this.quote = null;
        this.quotePreview = $('#composer-quote-preview');
        this.quotePreviewText = $('#composer-quote-preview-text');
        this.quotePreviewDismiss = $('#composer-quote-preview-dismiss');

        if (this.quotePreviewDismiss) {
            this.quotePreviewDismiss.addEventListener('click', () => this.clearQuote());
        }

        this.selectedIndex = 0;
        this.filteredItems = [];
        this.currentType = null;
        this.currentTrigger = null;
        this.suppressTriggerDetection = false;
        this.menuPageSize = 20;
        this.menuRenderedCount = 0;

        if (this.menu) {
            this.menu.addEventListener('scroll', () => this.handleMenuScroll());
        }

        if (!this.input || !this.sendBtn)
            throw new Error("ComposerView: missing input elements");

        this.inputMinHeight = parseFloat(window.getComputedStyle(this.input).minHeight) || 40;

        this.bindEvents();
    }
    /**
     * Updates the composer change callback after initialization.
     * This allows controllers to be wired after the view is created.
     *
     * @param {Function|null} callback
     */
    setOnChange(callback) {
        this.onChange = typeof callback === 'function' ? callback : null;
    }

    setOnContextClicked(callback) {
        this.onContextClicked = typeof callback === 'function' ? callback : null;
    }

    /**
     * Updates the composer send callback after initialization.
     *
     * @param {Function|null} callback
     */
    setOnSend(callback) {
        this.onSend = typeof callback === 'function' ? callback : null;
    }

    setOnCancel(callback) {
        this.onCancel = typeof callback === 'function' ? callback : null;
    }

    bindEvents() {

        this.sendBtn.addEventListener('click', () => this.send());

        this.input.addEventListener('keydown', (e) => {

            // Check if the composer menu is currently visible on the screen
            const menuVisible = document.querySelector('.composer-menu') && !document.querySelector('.composer-menu').classList.contains('hidden');

            // Handle actions when the Enter key is pressed
            if (e.key === 'Enter') {
                if (menuVisible) {
                    // Prevent sending the message or inserting a newline when the menu is open
                    e.preventDefault();

                    // Trigger the selection of the currently highlighted menu item
                    this.composerMenuSelect(this.currentType, this.filteredItems[this.selectedIndex], this.currentTrigger);
                } else if (!e.shiftKey) {
                    // Prevent the default behavior (new line) and send the message only if Shift is NOT held
                    e.preventDefault();
                    this.send();
                }
            }
            else if (e.key === 'ArrowDown' && menuVisible) {
                e.preventDefault();
                this.navigateMenu(1);
            }
            else if (e.key === 'ArrowUp' && menuVisible) {
                e.preventDefault();
                this.navigateMenu(-1);
            }
            else if (e.key === 'Backspace') {
                //const handled = this.handleBackspace();
                //if (handled) {
                //    e.preventDefault();
                //    return;
                //}
            }
            // Handle actions when the Escape key is pressed
            if (e.key === 'Escape') {
                if (menuVisible) {
                    e.preventDefault();
                    this.hideMenu();
                }
            }

        });

        this.input.addEventListener('input', () => {
            this.configInput();
        });

        this.input.addEventListener('paste', (e) => {
            this.handlePaste(e);
        });

        this.contextBtn.addEventListener('click', () => {
            var triggerData = {
                symbol: '+',
                type: TRIGGERS['+'],
                filter: '',
            };

            this.onContextClicked({
                trigger: triggerData
            });

            this.input.focus();
        });

        this.contextBtn.addEventListener('keydown', (e) => {

            // Check if the composer menu is currently visible on the screen
            const menuVisible = document.querySelector('.composer-menu') && !document.querySelector('.composer-menu').classList.contains('hidden');

            // Handle actions when the Enter key is pressed
            if (e.key === 'Enter') {
                if (menuVisible) {
                    // Prevent sending the message or inserting a newline when the menu is open
                    e.preventDefault();

                    // Trigger the selection of the currently highlighted menu item
                    this.composerMenuSelect(this.currentType, this.filteredItems[this.selectedIndex], this.currentTrigger);
                } else if (!e.shiftKey) {
                    // Prevent the default behavior (new line) and send the message only if Shift is NOT held
                    e.preventDefault();
                    this.send();
                }
            }
            else if (e.key === 'ArrowDown' && menuVisible) {
                e.preventDefault();
                this.navigateMenu(1);
            }
            else if (e.key === 'ArrowUp' && menuVisible) {
                e.preventDefault();
                this.navigateMenu(-1);
            }
            // Handle actions when the Escape key is pressed
            if (e.key === 'Escape') {
                if (menuVisible) {
                    e.preventDefault();
                    this.hideMenu();
                }
            }

        });

    }

    configInput() {
        this.updatePlaceholder();
        this.resize();
        this.updateSendState();
        this.notifyChange();
    }

    handlePaste(e) {
        e.preventDefault();

        const clipboardData = e.clipboardData || window.clipboardData;
        if (!clipboardData) return;

        const text = clipboardData.getData('text/plain');
        if (!text) return;

        this.insertPlainTextAtSelection(text);

        this.suppressTriggerDetection = true;
        try {
            this.configInput();
        } finally {
            this.suppressTriggerDetection = false;
        }
    }

    insertPlainTextAtSelection(text) {
        const input = this.input;
        input.focus();

        const selection = window.getSelection();
        let range = null;

        if (selection && selection.rangeCount > 0) {
            const selectedRange = selection.getRangeAt(0);

            if (input.contains(selectedRange.startContainer) && input.contains(selectedRange.endContainer)) {
                range = selectedRange;
            }
        }

        if (!range) {
            range = document.createRange();
            range.selectNodeContents(input);
            range.collapse(false);
        }

        range.deleteContents();

        const textNode = document.createTextNode(text);
        range.insertNode(textNode);

        range.setStartAfter(textNode);
        range.collapse(true);

        selection.removeAllRanges();
        selection.addRange(range);
    }

    send() {
        if (this.isStreaming) {
            if (this.onCancel)
                this.onCancel();

            return;
        }

        this.clear();

        if (this.onSend)
            this.onSend();
    }

    notifyChange() {

        if (!this.onChange) return;

        // If the content is completely empty (e.g. after Select All + Delete)
        if (this.getText() === '') {
            this.clear();
            this.onChange({
                text: this.getText(),
                cursor: 0,
                trigger: null
            });
            return;
        }

        const selection = window.getSelection();
        if (!selection.rangeCount) return;

        const range = selection.getRangeAt(0);

        // 1. Get the text before the cursor to detect the trigger
        const preCaretRange = range.cloneRange();
        preCaretRange.selectNodeContents(this.input);
        preCaretRange.setEnd(range.endContainer, range.endOffset);
        const textBeforeCursor = preCaretRange.toString();

        // 2. Detect trigger
        const triggerData = this.suppressTriggerDetection
            ? null
            : this.detectTrigger(textBeforeCursor, textBeforeCursor.length);

        if (triggerData) {
            // 3. Safely remove the trigger from the DOM
            const triggerRange = document.createRange();
            triggerRange.setStart(range.startContainer, range.startOffset - triggerData.length);
            triggerRange.setEnd(range.startContainer, range.startOffset);
            //triggerRange.deleteContents();
        }

        this.onChange({
            text: this.input.innerText,
            cursor: range.endOffset,
            trigger: triggerData
        });
    }

    insertChip(item) {
        this.input.focus();
        const selection = window.getSelection();
        if (!selection.rangeCount) return;

        const range = selection.getRangeAt(0);

        // Create a chip element
        const chip = document.createElement('span');
        chip.className = `composer-chip composer-chip--${item.type || 'default'}`;
        chip.contentEditable = 'false';
        // Render icon (if provided) and label
        chip.innerHTML = `${item.icon ? `<codinex-icon name="${item.icon}"></codinex-icon>` : ''}<span class="chip-label">${item.label || item.name || item.text}</span>`;
        chip.dataset.id = item.id;
        chip.dataset.type = item.type;
        chip.dataset.name = item.label || item.name || item.text

        // For example, if you want to add a space after the chip
        const node = range.startContainer;
        const text = node.textContent;
        const symbol = text.lastIndexOf(item.trigger.symbol, range.startOffset);
        if (symbol !== -1) {
            range.setStart(node, symbol);
            range.deleteContents();
        }

        const spaceNode = document.createTextNode('\u00A0');

        range.insertNode(spaceNode);
        range.insertNode(chip);

        // Set the cursor position after the chip
        range.setStartAfter(spaceNode);
        range.collapse(true);
        selection.removeAllRanges();
        selection.addRange(range);
    }

    resize() {

        this.input.style.height = 'auto';
        this.input.style.height = Math.min(this.input.scrollHeight, 500) + 'px';

    }

    resetHeight() {
        this.input.style.height = this.inputMinHeight + 'px';
    }

    updateSendState() {
        if (this.isStreaming) {
            togglePanelDisable('#send-btn', true);
            return;
        }

        togglePanelDisable('#send-btn', this.input.innerText.trim() !== '');
    }

    setStreaming(isStreaming) {
        this.isStreaming = !!isStreaming;

        if (!this.sendBtn) return;

        this.sendBtn.classList.toggle('streaming-stop', this.isStreaming);
        this.sendBtn.title = this.isStreaming ? 'Stop' : 'Send';
        this.sendBtn.innerHTML = this.isStreaming
            ? '<codinex-icon name="symbols/stop-circle"></codinex-icon>'
            : '<codinex-icon name="Actions/send-horizontal"></codinex-icon>';

        this.setInputDisabled(this.isStreaming);
        this.updateSendState();
    }

    setInputDisabled(isDisabled) {
        if (this.inputContainer) {
            this.inputContainer.classList.toggle('responding-disabled', isDisabled);
            this.inputContainer.setAttribute('aria-disabled', isDisabled ? 'true' : 'false');
        }

        if (this.input) {
            this.input.contentEditable = isDisabled ? 'false' : 'true';
        }

        if (this.contextInput) {
            this.contextInput.contentEditable = isDisabled ? 'false' : 'true';
        }

        const disabledSelectors = [
            '#contextBtn',
            '#settings-btn',
            '#model-selector-btn',
            '#agent-selector-btn',
            '.context-chip-remove',
            '.context-chip-text'
        ];

        disabledSelectors.forEach(selector => {
            document.querySelectorAll(selector).forEach(element => {
                element.disabled = isDisabled;
                element.setAttribute('aria-disabled', isDisabled ? 'true' : 'false');
            });
        });

        if (isDisabled) {
            this.hideMenu();
        }
    }

    setText(text) {

        this.input.innerText = text || '';
        this.updatePlaceholder();
        this.resize();
        this.updateSendState();

    }

    clear() {

        this.input.innerHTML = '';
        this.resetHeight();
        this.updateSendState();
        this.hideMenu();

    }

    updatePlaceholder() {
        // Check if there is any actual text or any chips
        const text = this.getText();
        const hasChips = this.input.querySelectorAll('.chip').length > 0;

        if (!text && !hasChips) {
            this.input.classList.add('is-empty');
        } else {
            this.input.classList.remove('is-empty');
        }
    }

    getText() {
        return this.input.innerText.trim();
    }

    detectTrigger(text, cursor) {
        // Slice text from start to current cursor position
        const before = text.slice(0, cursor);

        // Look for the last symbol followed by word characters at the end of the string
        // Group 1: Symbol (/@#)
        // Group 2: Potential type (e.g., 'folder')
        // Group 3: Optional colon and subsequent filter text
        const regex = /([/@#])([a-zA-Z0-9_-]*)(?::([a-zA-Z0-9_-]*))?$/;
        const match = before.match(regex);

        if (!match) return null;

        const symbol = match[1];
        let typeFilter = null;
        let filterText = '';

        // If there is a third group (content after colon), it means #type:filter was used
        if (match[3] !== undefined) {
            typeFilter = match[2]; // e.g., 'folder'
            filterText = match[3]; // e.g., 'src'
        } else {
            filterText = match[2] || ''; // standard #filter syntax
        }

        return {
            symbol,
            type: TRIGGERS[symbol],
            filter: filterText,
            typeFilter: typeFilter,
            start: before.lastIndexOf(symbol),
            end: cursor
        };
    }

    showMenu(items = [], type, trigger) {

        if (!this.menu) return;

        this.filteredItems = items;
        this.selectedIndex = 0; // Reset to first item whenever list changes
        this.currentType = type;
        this.currentTrigger = trigger;
        this.menuRenderedCount = 0;

        // mark menu with the current section type (commands/agents/references)
        this.menu.setAttribute('data-section', type);
        this.menu.innerHTML = '';
        this.menu.scrollTop = 0;
        this.menu.classList.remove('hidden');

        this.renderNextMenuPage();

    }

    // Long generated/migration file names (e.g. "202412301245493_Make_Nullable_Status_In_AspNetUsers.Designer.cs")
    // blow out the menu row width. Truncate in the middle so the meaningful prefix and the
    // extension/suffix (".Designer.cs") both stay visible instead of the tail getting cut off.
    truncateMiddle(text, maxLength = 45) {
        if (!text || text.length <= maxLength) return text;

        const keepEnd = 20;
        const keepStart = Math.max(maxLength - keepEnd - 3, 8);

        return `${text.slice(0, keepStart)}...${text.slice(text.length - keepEnd)}`;
    }

    renderNextMenuPage() {

        if (!this.menu) return;

        const start = this.menuRenderedCount;
        const pageSize = this.currentType === 'references' ? this.menuPageSize : this.filteredItems.length;
        const end = Math.min(start + pageSize, this.filteredItems.length);

        for (let index = start; index < end; index++) {

            const item = this.filteredItems[index];
            const el = document.createElement('button');
            const fullName = item.label || item.name;

            el.className = `composer-menu-item  ${index === this.selectedIndex ? 'active' : ''}`;

            el['data-index'] = index;

            el.innerHTML = `
                ${item.icon ? `<div class="item-icon" style="${item.color ? `color: var(${item.color});` : ''}"><codinex-icon name="${item.icon}"></codinex-icon></div>` : ''}
                <div class="item-name" title="${fullName}">${this.truncateMiddle(fullName)}</div>
                ${item.description ? `<div class="item-desc">${typeof (item.description) == 'function' ? item.description() : item.description}</div>` : ''}
            `;

            el.addEventListener('click', () => {
                this.composerMenuSelect(this.currentType, item, this.currentTrigger);
            });

            this.menu.appendChild(el);
        }

        this.menuRenderedCount = end;
    }

    handleMenuScroll() {

        if (!this.menu || this.currentType !== 'references') return;
        if (this.menuRenderedCount >= this.filteredItems.length) return;

        const scrollThreshold = 20;
        const isAtBottom = this.menu.scrollTop + this.menu.clientHeight >= this.menu.scrollHeight - scrollThreshold;

        if (isAtBottom) {
            this.renderNextMenuPage();
        }

    }
    composerMenuSelect(type, item, trigger) {
        document.dispatchEvent(new CustomEvent('composer:menu-select', {
            detail: { type, item, trigger }
        }));
    }

    hideMenu() {

        if (!this.menu) return;

        this.menu.classList.add('hidden');
        this.menu.innerHTML = '';
        this.menuRenderedCount = 0;

    }
    navigateMenu(direction) {
        let items = this.menu ? this.menu.querySelectorAll('.composer-menu-item') : document.querySelectorAll('.composer-menu-item');
        if (items.length === 0) return;

        // Remove active class from old item
        items[this.selectedIndex].classList.remove('active');

        // Calculate new index
        this.selectedIndex += direction;

        if (this.selectedIndex >= items.length && this.menuRenderedCount < this.filteredItems.length) {
            this.renderNextMenuPage();
            items = this.menu.querySelectorAll('.composer-menu-item');
        }

        // Boundary checks (Looping)
        if (this.selectedIndex < 0) this.selectedIndex = items.length - 1;
        if (this.selectedIndex >= items.length) this.selectedIndex = 0;

        // Add active class to new item and scroll into view
        const activeItem = items[this.selectedIndex];
        activeItem.classList.add('active');
        activeItem.scrollIntoView({ block: 'nearest' });
    }

    selectCurrentItem() {
        const selectedData = this.filteredItems[this.selectedIndex];
        if (selectedData) {
            this.applySelection(selectedData.name); // Your existing logic to insert text
        }
    }
    handleBackspace() {
        const selection = window.getSelection();
        if (!selection || !selection.rangeCount) return false;

        const range = selection.getRangeAt(0);
        if (!range.collapsed) return false;

        let nodeToDelete = null;
        const container = range.startContainer;
        const offset = range.startOffset;

        // Case 1: Caret is at the very beginning of a text node
        if (container.nodeType === Node.TEXT_NODE && offset === 0) {
            nodeToDelete = container.previousSibling;
        }
        // Case 2: Caret is in a text node, but there's only whitespace behind it
        else if (container.nodeType === Node.TEXT_NODE && offset > 0) {
            const textBefore = container.textContent.substring(0, offset);
            // If the only thing before the cursor in this node is a space/newline
            if (textBefore.trim() === '' && textBefore.length > 0) {
                // Manually clear this whitespace to "jump" to the chip
                container.textContent = container.textContent.substring(offset);
                nodeToDelete = container.previousSibling.previousSibling;
            }
        }
        // Case 3: Caret is directly in the parent container
        else if (container === this.input) {
            nodeToDelete = this.input.childNodes[offset - 1];
        }

        // Validation: Is it really a chip?
        if (nodeToDelete && nodeToDelete.nodeType === Node.ELEMENT_NODE &&
            nodeToDelete.classList.contains('composer-chip')) {

            // Prepare data for removal
            const chipData = {
                id: nodeToDelete.getAttribute('data-id'),
                type: nodeToDelete.getAttribute('data-type'),
                name: nodeToDelete.getAttribute('data-name')
            };

            // Dispatch removal to controller
            document.dispatchEvent(new CustomEvent('composer:chip-remove', {
                detail: chipData
            }));

            // 2. Remove from UI
            nodeToDelete.remove();

            // 3. Clean up the DOM (merges adjacent text nodes)
            this.input.normalize();

            return true; // Stop browser from doing default backspace
        }

        return false;
    }
    /**
     * Extracts a plain text representation from the contenteditable composer.
     * Chips are converted to their textual value instead of being removed.
     */
    getPlainText() {
        if (!this.input) {
            return '';
        }

        const tempDiv = this.input.cloneNode(true);

        const chips = tempDiv.querySelectorAll('.composer-chip');

        chips.forEach((chip) => {
            const chipText =
                chip.dataset.name ||
                chip.querySelector('.chip-label')?.textContent ||
                chip.textContent ||
                '';

            const textNode = document.createTextNode(chipText);

            chip.replaceWith(textNode);
        });

        return this.normalizeComposerText(tempDiv.textContent || '');
    }

    /**
     * Normalizes composer plain text while preserving meaningful user content.
     */
    normalizeComposerText(text) {
        return text
            .replace(/\u00A0/g, ' ')
            .replace(/[ \t]+/g, ' ')
            .replace(/\n{3,}/g, '\n\n')
            .trim();
    }

    /**
     * Renders the reference chips based on the selectedReferences array.
     * Clears old dynamic chips and renders current ones.
     */
    updateReferenceChips(selectedReferences) {
        const contextRow = document.querySelector('.input-context-row');
        const contextInput = document.getElementById('contextInput');

        // 1. Remove existing dynamic chips to avoid duplicates
        // Assuming dynamic chips have a class '.dynamic-chip'
        const existingChips = contextRow.querySelectorAll('.dynamic-chip');
        existingChips.forEach(chip => chip.remove());

        // 2. Iterate and create new chips
        selectedReferences.forEach(ref => {
            const refId = ref.id || ref.Id;
            const refName = ref.name || ref.Name || '';
            const refDescription = ref.description || ref.Description || '';
            const refIcon = ref.icon || ref.Icon;
            const refColor = ref.color || ref.Color;
            const refMetadata = ref.metadata || ref.Metadata || {};
            const refFilePath = refMetadata.filePath || refMetadata.FilePath || ref.value || ref.Value || '';
            const chipTitle = refName === 'Active Document' && refDescription
                ? `Active Document: ${refDescription}`
                : (refDescription || refFilePath || refName);

            const chip = document.createElement('div');
            chip.className = 'context-chip dynamic-chip';
            chip.title = chipTitle;
            chip.innerHTML = `
                ${refIcon ? `<div class="item-icon" style="${refColor ? `color: var(${refColor});` : ''}"><codinex-icon name="${refIcon}"></codinex-icon></div>` : ''}
                <span class="context-chip-text" role="button" tabindex="0" data-id="${refId}">${refName}</span>
                <button class="context-chip-remove" title="Remove Context" data-id="${refId}">
                    <codinex-icon name="Actions/circle-x"></codinex-icon>
                </button>`;

            const chipText = chip.querySelector('.context-chip-text');
            const handleChipTextClick = (e) => {
                e.stopPropagation();

                if (chipText.getAttribute('aria-disabled') === 'true') return;

                document.dispatchEvent(new CustomEvent('composer:ref-click', {
                    detail: { id: refId, reference: ref }
                }));
            };

            chipText.addEventListener('click', handleChipTextClick);

            // 3. Attach remove event
            chip.querySelector('.context-chip-remove').addEventListener('click', (e) => {
                e.stopPropagation();
                document.dispatchEvent(new CustomEvent('composer:ref-remove', {
                    detail: { id: refId }
                }));
            });

            // Insert before the add button
            contextRow.insertBefore(chip, contextInput.nextSibling);
        });

        // Remove typed filter text before converting 
        this.removeTypedFilterFromCaret();

    }
    removeTypedFilterFromCaret() {
        const input = this.input;
        const selection = window.getSelection();

        if (!selection || selection.rangeCount === 0) return;

        let range = selection.getRangeAt(0);

        // Ensure we're inside composer
        if (!input.contains(range.startContainer)) return;

        // If startContainer is an element node, try to resolve to a text node
        let container = range.startContainer;
        let offset = range.startOffset;

        if (container.nodeType === Node.ELEMENT_NODE) {
            // Find the text node before caret position
            const child = container.childNodes[offset - 1];
            if (child && child.nodeType === Node.TEXT_NODE) {
                container = child;
                offset = child.textContent.length;
            } else {
                // Try next/first text node
                const walker = document.createTreeWalker(
                    input,
                    NodeFilter.SHOW_TEXT,
                    null
                );

                let textNode = null;
                while (walker.nextNode()) {
                    textNode = walker.currentNode;
                    if (textNode.parentNode === container) {
                        break;
                    }
                }

                if (!textNode) return;
                container = textNode;
                offset = textNode.textContent.length;
            }
        }

        const text = container.textContent || '';
        const hashIndex = text.lastIndexOf('#', offset);

        if (hashIndex === -1) return;

        // Remove from # to caret
        const deleteRange = document.createRange();
        deleteRange.setStart(container, hashIndex);
        deleteRange.setEnd(container, offset);
        deleteRange.deleteContents();

        // Restore caret
        const caretRange = document.createRange();
        caretRange.setStart(container, hashIndex);
        caretRange.collapse(true);
        selection.removeAllRanges();
        selection.addRange(caretRange);
    }
    removeRefNode(refId) {
        // Reference ids are now content-derived (e.g. "file:C:\path\to\file.cs"), so they can
        // contain backslashes/colons. CSS attribute selectors treat backslash as an escape
        // character, so interpolating the raw id into a selector string silently fails to match
        // (querySelector finds nothing) instead of throwing. CSS.escape() is required here.
        //
        // Content-derived ids also mean the same reference can appear as more than one chip
        // (e.g. picked both as "Active Document" and from the file list). State removal drops
        // every entry with this id, so the DOM must remove every matching chip too, not just
        // the first one found.
        const chips = document.querySelectorAll(`.context-chip-remove[data-id="${CSS.escape(refId)}"]`);

        chips.forEach(chip => chip.parentNode?.remove());
    }

    insertTextAtCursor(filter) {
        const text = ' #' + filter + ':';
        const input = this.input;
        input.focus();

        const selection = window.getSelection();

        if (!selection || selection.rangeCount === 0) {
            input.appendChild(document.createTextNode(text));
            this.moveCaretToEnd();
            this.configInput();
            return;
        }

        const range = selection.getRangeAt(0);

        if (!input.contains(range.startContainer)) {
            input.appendChild(document.createTextNode(text));
            this.moveCaretToEnd();
            this.configInput();
            return;
        }

        range.deleteContents();

        const textNode = document.createTextNode(text);
        range.insertNode(textNode);

        // Place caret after inserted node
        range.setStartAfter(textNode);
        range.collapse(true);
        selection.removeAllRanges();
        selection.addRange(range);

        this.configInput();
    }


    moveCaretToEnd() {
        const input = this.input;
        const selection = window.getSelection();
        const range = document.createRange();

        range.selectNodeContents(input);
        range.collapse(false);

        selection.removeAllRanges();
        selection.addRange(range);
    }

    /**
     * Attaches a quoted assistant selection to the next outgoing message and
     * shows a dismissible preview above the composer. Used by the "Ask Codinex"
     * action on the assistant-message selection toolbar.
     */
    setQuote(text) {
        const trimmed = (text || '').trim();

        if (!trimmed) return;

        this.quote = trimmed;

        if (this.quotePreviewText) this.quotePreviewText.textContent = trimmed;
        if (this.quotePreview) this.quotePreview.classList.remove('hidden');

        this.input.focus();
    }

    clearQuote() {
        this.quote = null;

        if (this.quotePreviewText) this.quotePreviewText.textContent = '';
        if (this.quotePreview) this.quotePreview.classList.add('hidden');
    }

    getQuote() {
        return this.quote;
    }

}
