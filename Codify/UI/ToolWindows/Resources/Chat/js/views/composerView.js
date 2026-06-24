/**
 * ComposerView
 * Handles message input, triggers, chips and command menus.
 * No AI logic should exist here.
 */

import { $, togglePanelDisable, togglePanelHidden } from '../utils/dom.js';

const TRIGGERS = {
    '/': 'command',
    '@': 'agent',
    '#': 'reference'
};

export class ComposerView {

    constructor({ onSend, onChange }) {

        this.onSend = onSend;
        this.onChange = onChange;

        this.input = $('#userInput');
        this.sendBtn = $('#send-btn');
        this.chipsContainer = $('#composer-chips') || document.querySelector('.composer-chips');
        this.menu = $('#composer-menu') || document.querySelector('.composer-menu');

        this.selectedIndex = 0;
        this.filteredItems = []; 

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
                    this.composerMenuSelect(this.currentType, this.filteredItems[this.selectedIndex]);
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

        this.input.addEventListener('input', () => {

            this.resize();
            this.updateSendState();
            this.notifyChange();

        });
    }

    send() {

        const text = this.input.value.trim();

        if (!text) return;

        this.input.value = '';

        this.resetHeight();
        this.updateSendState();
        this.hideMenu();

        if (this.onSend)
            this.onSend(text);
    }

    notifyChange() {

        if (!this.onChange) return;

        const cursor = this.input.selectionStart || 0;

        this.onChange({
            text: this.input.value,
            cursor,
            trigger: this.detectTrigger(this.input.value, cursor)
        });
    }

    resize() {

        this.input.style.height = 'auto';

        if (this.input.scrollHeight > 500)
            this.input.style.height = '500px';
        else
            this.input.style.height = this.input.scrollHeight + 'px';

    }

    resetHeight() {
        this.input.style.height = this.inputMinHeight + 'px';
    }

    updateSendState() {
        togglePanelDisable('#send-btn', this.input.value.trim() !== '');
    }

    setText(text) {

        this.input.value = text || '';
        this.resize();
        this.updateSendState();

    }

    clear() {

        this.input.value = '';
        this.resetHeight();
        this.updateSendState();
        this.hideMenu();

    }

    getText() {
        return this.input.value.trim();
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

        const typeMap = {
            '/': 'commands',
            '@': 'agents',
            '#': 'references'
        };

        return {
            symbol,
            type: typeMap[symbol],
            filter: filterText,
            start: before.lastIndexOf(symbol),
            end: cursor
        };
    }


    renderChips(items = []) {

        if (!this.chipsContainer) return;

        this.chipsContainer.innerHTML = '';

        for (const item of items) {

            const chip = document.createElement('button');

            chip.className = `composer-chip composer-chip--${item.type || 'default'}`;

            chip.innerHTML = `
                <span>${item.label || item.name}</span>
                <span class="remove">×</span>
            `;

            chip.addEventListener('click', () => {

                document.dispatchEvent(new CustomEvent('composer:chip-remove', {
                    detail: item
                }));

            });

            this.chipsContainer.appendChild(chip);

        }
    }

    showMenu(items = [], type) {

        if (!this.menu) return;

        this.filteredItems = items;
        this.selectedIndex = 0; // Reset to first item whenever list changes
        this.currentType = type;

        this.menu.innerHTML = '';
        this.menu.classList.remove('hidden');

        var index = 0;

        for (const item of items) {

            const el = document.createElement('button');

            el.className = `composer-menu-item  ${index === 0 ? 'active' : ''}`;

            el['data-index'] = index;

            el.innerHTML = `
                <div class="item-name">${item.label || item.name}</div>
                ${item.description ? `<div class="item-desc">${item.description}</div>` : ''}
            `;

            el.addEventListener('click', () => {
                this.composerMenuSelect(type, item);
            });

            this.menu.appendChild(el);
            index++;
        }

    }
    composerMenuSelect(type, item) {
        document.dispatchEvent(new CustomEvent('composer:menu-select', {
            detail: { type, item }
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
}
