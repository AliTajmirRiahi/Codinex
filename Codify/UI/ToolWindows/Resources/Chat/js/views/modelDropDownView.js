import { $ } from '../utils/dom.js';


export const modelDropDownView = {
    // Internal state to manage models across pages
    state: {
        selectedModels: new Map(), // Stores full model objects to maintain 
    },

    initEventHandlers() {
        // Event Listeners for Pager Buttons

        const modelBtn = $('#model-selector-btn');
        const modelMenu = $('#model-dropdown-menu');
        const modelNameSpan = $('#active-model-name');
        const modelOptions = document.querySelectorAll('.model-option');

        modelBtn.onclick = (e) => {
            e.stopPropagation();
            modelMenu.classList.toggle('show');
        };

        // Close Menu when clicking anywhere else
        document.addEventListener('click', (e) => {
            if (!modelMenu.contains(e.target) && e.target !== modelBtn) {
                modelMenu.classList.remove('show');
            }
        });

        //$('#next-page').onclick = () => {
        //    const totalPages = this.pagination.getTotalPages();
        //    if (this.pagination.currentPage < totalPages) {
        //        this.pagination.nextPage();
        //        this.renderModelPage();
        //    }
        //};

        //$('#save-settings-btn').onclick = () => {
        //    const data = {
        //        providerId: $('#provider-select').value,
        //        selectedModels: Array.from(this.state.selectedModels.values()),
        //        apiKey: $('#model-api-key').value,
        //    };
        //    const validation = validationService.validate(data, {
        //        rules: [
        //            { field: 'providerId', validator: validationService.isSelected, message: 'Please select a provider.', mode: 'inline', target: '#provider-select' },
        //            { field: 'selectedModels', validator: validationService.hasSelectedItems, message: 'Please select at least one model.', mode: 'inline', target: '#models-checkbox-list' },
        //            { field: 'apiKey', validator: validationService.isNotEmpty, message: 'API key is required.', mode: 'inline', target: '#model-api-key' }
        //        ],
        //    });

        //    if (!validation.valid) {
        //        validationService.showErrors(validation.errors);
        //        return;
        //    }

        //    if (saveCallBack)
        //        saveCallBack(data);
        //};
    },
    /**
     * Renders the provider dropdown and attaches selection logic.
     */
    renderProvider(provider, selectedModels) {
        const placeholder = $('#model-dropdown-menu-placeholder');
        if (!placeholder) return;

        placeholder.innerHTML = '';

        selectedModels.forEach(p => {
            var menu = this.createModelOptionElement(p, false);
            placeholder.appendChild(menu);
        });
    },
    /**
     * Creates a model option element for the dropdown
     * @param {Object} model - Model data { id, name, icon, multiplier }
     * @param {boolean} isActive - Whether this model is currently selected
     * @returns {HTMLElement}
     * @private
     */
    createModelOptionElement(model, isActive = false) {
        // Create the main container
        const optionDiv = document.createElement('div');
        optionDiv.className = `model-option${isActive ? ' active' : ''}`;
        optionDiv.setAttribute('data-value', model.id);

        // Create the model-info container (Left side)
        const infoDiv = document.createElement('div');
        infoDiv.className = 'model-info';

        // Create and append the Codify Icon
        const icon = document.createElement('codify-icon');
        icon.setAttribute('name', model.icon || 'lightning');
        icon.className = 'low-vis';
        infoDiv.appendChild(icon);

        // Create and append the Model Name
        const nameSpan = document.createElement('span');
        nameSpan.textContent = model.name;
        infoDiv.appendChild(nameSpan);

        optionDiv.appendChild(infoDiv);

        // Create and append the Multiplier (Right side)
        const multiplierSpan = document.createElement('span');
        multiplierSpan.className = 'multiplier';
        multiplierSpan.textContent = model.multiplier || '1x';
        optionDiv.appendChild(multiplierSpan);

        return optionDiv;
    },
    /**
     * Shows the dropdown
     */
    show() {
    },

    /**
     * Hides the dropdown
     */
    hide() {
    },
};
