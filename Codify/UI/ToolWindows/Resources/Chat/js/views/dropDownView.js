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
                if (this.onItemSelect) this.onItemSelect(item);
                this.hide();
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


//export const modelDropDownView = {
//    // Internal state to manage models across pages
//    state: {
//        selectedModels: new Map(), // Stores full model objects to maintain 
//    },

//    initEventHandlers() {
//        // Event Listeners for Pager Buttons

//        const modelBtn = $('#model-selector-btn');
//        const modelMenu = $('#model-dropdown-menu');
//        const modelNameSpan = $('#active-model-name');
//        const modelOptions = document.querySelectorAll('.model-option');

//        modelBtn.onclick = (e) => {
//            e.stopPropagation();
//            modelMenu.classList.toggle('show');
//        };

//        // Close Menu when clicking anywhere else
//        document.addEventListener('click', (e) => {
//            if (!modelMenu.contains(e.target) && e.target !== modelBtn) {
//                modelMenu.classList.remove('show');
//            }
//        });
        
//    },
//    /**
//     * Renders the provider dropdown and attaches selection logic.
//     */
//    renderProvider(provider, selectedModels) {
//        const placeholder = $('#model-dropdown-menu-placeholder');
//        if (!placeholder) return;

//        placeholder.innerHTML = '';

//        selectedModels.forEach(p => {
//            var menu = this.createModelOptionElement(p, false);
//            placeholder.appendChild(menu);
//        });
//    },
//    /**
//     * Creates a model option element for the dropdown
//     * @param {Object} model - Model data { id, name, icon, multiplier }
//     * @param {boolean} isActive - Whether this model is currently selected
//     * @returns {HTMLElement}
//     * @private
//     */
//    createModelOptionElement(model, isActive = false) {
//        // Create the main container
//        const optionDiv = document.createElement('div');
//        optionDiv.className = `model-option${isActive ? ' active' : ''}`;
//        optionDiv.setAttribute('data-value', model.id);

//        // Create the model-info container (Left side)
//        const infoDiv = document.createElement('div');
//        infoDiv.className = 'model-info';

//        // Create and append the Codify Icon
//        const icon = document.createElement('codify-icon');
//        icon.setAttribute('name', model.icon || 'lightning');
//        icon.className = 'low-vis';
//        infoDiv.appendChild(icon);

//        // Create and append the Model Name
//        const nameSpan = document.createElement('span');
//        nameSpan.textContent = model.name;
//        infoDiv.appendChild(nameSpan);

//        optionDiv.appendChild(infoDiv);

//        // Create and append the Multiplier (Right side)
//        const multiplierSpan = document.createElement('span');
//        multiplierSpan.className = 'multiplier';
//        multiplierSpan.textContent = model.multiplier || '1x';
//        optionDiv.appendChild(multiplierSpan);

//        return optionDiv;
//    },
//    /**
//     * Shows the dropdown
//     */
//    show() {
//    },

//    /**
//     * Hides the dropdown
//     */
//    hide() {
//    },
//};
