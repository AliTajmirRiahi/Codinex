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

    constructor({ onSend, onChange }) {

        this.onSend = onSend;
        this.onChange = onChange;

        this.input = $('#userInput');
        this.sendBtn = $('#send-btn');
        this.chipsContainer = $('#composer-chips') || document.querySelector('.composer-chips');
        this.menu = $('#composer-menu') || document.querySelector('.composer-menu');
        this.contextBtn = $('#contextBtn') || document.querySelector('#contextBtn');
        this.contextInput = $('#contextInput') || document.querySelector('#contextInput');

        this.selectedIndex = 0;
        this.filteredItems = [];
        this.currentType = null;
        this.currentTrigger = null;

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

        this.contextBtn.addEventListener('click', () => {
            var triggerData = {
                symbol: '+',
                type: TRIGGERS['+'],
                filter: '',
            };

            this.onContextClicked({
                trigger: triggerData
            });
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

    send() {

        const text = this.input.innerText.trim();

        if (!text) return;

        this.clear();

        if (this.onSend)
            this.onSend(text);
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
        const triggerData = this.detectTrigger(textBeforeCursor, textBeforeCursor.length);

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
        chip.innerHTML = `${item.icon ? `<codify-icon name="${item.icon}"></codify-icon>` : ''}<span class="chip-label">${item.label || item.name || item.text}</span>`;
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
        togglePanelDisable('#send-btn', this.input.innerText.trim() !== '');
    }

    setText(text) {

        this.input.innerText = text || '';
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
        const regex = /([/@#])([a-zA-Z0-9_-]*)$/;
        const match = before.match(regex);

        if (!match) return null;

        const symbol = match[1];
        const filterText = match[2] || ''; // match[2] will be the text after symbol

        return {
            symbol,
            type: TRIGGERS[symbol],
            filter: filterText,
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

        // mark menu with the current section type (commands/agents/references)
        this.menu.setAttribute('data-section', type);
        this.menu.innerHTML = '';
        this.menu.classList.remove('hidden');

        var index = 0;

        for (const item of items) {

            const el = document.createElement('button');

            el.className = `composer-menu-item  ${index === 0 ? 'active' : ''}`;

            el['data-index'] = index;

            el.innerHTML = `
                ${item.icon ? `<div class="item-icon"><codify-icon name="${item.icon}"></codify-icon></div>` : ''}
                <div class="item-name">${item.label || item.name}</div>
                ${item.description ? `<div class="item-desc">${typeof (item.description) == 'function' ? item.description() : item.description}</div>` : ''}
            `;

            el.addEventListener('click', () => {
                this.composerMenuSelect(type, item, trigger);
            });

            this.menu.appendChild(el);
            index++;
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

    }
    navigateMenu(direction) {
        const items = document.querySelectorAll('.composer-menu-item');
        if (items.length === 0) return;

        // Remove active class from old item
        items[this.selectedIndex].classList.remove('active');

        // Calculate new index
        this.selectedIndex += direction;

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
            const chip = document.createElement('div');
            chip.className = 'context-chip dynamic-chip';
            chip.innerHTML = `
                <codify-icon name="${ref.icon}"></codify-icon>
                <span class="context-chip-text">${ref.name}</span>
                <button class="context-chip-remove" title="Remove Context" data-id="${ref.id}">
                    <codify-icon name="circle-x"></codify-icon>
                </button>`;

            // 3. Attach remove event
            chip.querySelector('.context-chip-remove').addEventListener('click', (e) => {
                e.stopPropagation();
                //// Call the controller method to remove from state and re-render
                //window.composerController.removeReference(ref.id);
                document.dispatchEvent(new CustomEvent('composer:ref-remove', {
                    detail: { id: ref.id }
                }));
            });

            // Insert before the add button
            contextRow.insertBefore(chip, contextInput.nextSibling);
        });

        const selection = window.getSelection();

        if (!selection.rangeCount) return;

        const range = selection.getRangeAt(0);

        const node = range.startContainer;
        const text = node.textContent;
        const symbol = text.lastIndexOf('#', range.startOffset);
        if (symbol !== -1) {
            range.setStart(node, symbol);
            range.deleteContents();
        }

    }

    removeRefNode(refId) {
        const chip = document.querySelector(`.context-chip-remove[data-id="${refId}"]`);

        if (!chip) return;

        chip.parentNode.remove();
    }

    insertTextAtCursor(filter) {
        const text = ' #' + filter;
        const input = this.input;
        input.focus();

        const selection = window.getSelection();

        // Fallback: if there is no valid selection, append using DOM nodes
        if (!selection || selection.rangeCount === 0) {
            input.appendChild(document.createTextNode(text));
            this.moveCaretToEnd();
            this.configInput();
            return;
        }

        const range = selection.getRangeAt(0);

        // Ensure insertion happens inside the composer
        if (!input.contains(range.startContainer)) {
            input.appendChild(document.createTextNode(text));
            this.moveCaretToEnd();
            this.configInput();
            return;
        }

        range.deleteContents();

        const textNode = document.createTextNode(text);
        range.insertNode(textNode);

        // Move caret to the end of the inserted text
        const newRange = document.createRange();
        newRange.setStartAfter(textNode);
        newRange.collapse(true);

        selection.removeAllRanges();
        selection.addRange(newRange);

        this.moveCaretToEnd();
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



}   
