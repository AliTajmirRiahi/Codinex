// Codify\UI\ToolWindows\Resources\Chat\js\views\dropDownView.js

import { $ } from '../utils/dom.js';

export class DropDownView {
    /**
     * @param {Object} options 
     * @param {string} options.containerId - The ID of the button/trigger element
     * @param {string} options.menuId - The ID of the menu element
     * @param {string} options.menuButtonId - The ID of menu button
     * @param {Function} options.onItemSelect - Callback when an item is clicked
     * @param {Function} options.itemTemplate - (Optional) Custom function to render each item
     */
    constructor(options) {
        this.container = $(`#${options.containerId}`);
        this.menu = $(`#${options.menuId}`);
        this.menuButton = $(`#${options.menuButtonId}`);
        this.onItemSelect = options.onItemSelect;
        this.itemTemplate = options.itemTemplate || this.defaultItemTemplate;

        this.isOpen = false;
        this.initEventHandlers();
    }

    initEventHandlers() {
        // Toggle menu visibility
        this.menuButton?.addEventListener('click', (e) => {
            e.stopPropagation();
            this.toggle();
        });

        // Close menu when clicking outside
        document.addEventListener('click', (e) => {
            if (this.isOpen && !this.menu.contains(e.target) && !this.container.contains(e.target)) {
                this.hide();
            }
        });

        //listening to call for close
        window.addEventListener('ui:close-all-dropdowns', () => {
            if (this.isOpen) this.hide();
        });
    }

    toggle() {
        this.isOpen ? this.hide() : this.show();
    }

    show() {
        this.isOpen = true;
        this.menu.classList.add('show');
        // Custom event for when menu opens
        this.menu.dispatchEvent(new CustomEvent('dropdown:opened'));
    }

    hide() {
        this.isOpen = false;
        this.menu.classList.remove('show');
    }

    /**
     * Renders the dynamic list of items
     * @param {Array} items - List of objects to display
     * @param {string|number} selectedValue - Currently active value
     */
    render(items, selectedValue) {
        if (!this.container) return;

        // Clear existing items
        this.container.innerHTML = '';

        items.forEach(item => {
            const isActive = item.id === selectedValue;
            const element = this.itemTemplate(item, isActive);

            // Add click listener to each option
            element.addEventListener('click', () => {
                if (this.onItemSelect) {
                    const success = this.onItemSelect(item);

                    if (success) {
                        this.hide();
                        this.render(items, item.id); // Re-render to update active state
                    }
                }
            });

            this.container.appendChild(element);
        });
    }

    /**
     * Default template for an item (Based on your previous UI)
     */
    defaultItemTemplate(item, isActive) {
        const option = document.createElement('div');
        option.className = `dropdown-option ${isActive ? 'active' : ''}`;
        option.dataset.value = item.id;

        option.innerHTML = `
            <div class="option-info">
                <codify-icon name="${item.icon || 'lightning'}"></codify-icon>
                <span class="option-name">${item.name}</span>
                ${item.multiplier ? `<span class="multiplier">${item.multiplier}</span>` : ''}
            </div>
        `;
        return option;
    }
}