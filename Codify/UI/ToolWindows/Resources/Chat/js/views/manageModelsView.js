/* 
 * path: Codify\UI\ToolWindows\Resources\Chat\js\views\manageModelsView.js
 */
import { $, addDefaultOption, togglePanelHidden, trigger } from '../utils/dom.js';
import { PaginationService } from '../services/paginationService.js';
import { validationService } from '../services/validationService.js';

/**
 * Manages the settings panel UI, including provider selection, 
 * model pagination, and state persistence for selections.
 */
export const manageModelsView = {
    // Internal state to manage models across pages
    state: {
        selectedModels: new Map(), // Stores full model objects to maintain 
    },
    // We now store a pagination instance instead of raw pagination values
    pagination: new PaginationService([], 5),

    initEventHandlers(saveCallBack) {
        // Event Listeners for Pager Buttons
        $('#prev-page').onclick = () => {
            if (this.pagination.currentPage > 1) {
                this.pagination.prevPage();
                this.renderModelPage();
            }
        };

        $('#next-page').onclick = () => {
            const totalPages = this.pagination.getTotalPages();
            if (this.pagination.currentPage < totalPages) {
                this.pagination.nextPage();
                this.renderModelPage();
            }
        };

        $('#save-settings-btn').onclick = () => {
            const data = {
                providerId: $('#provider-select').value,
                selectedModels: Array.from(this.state.selectedModels.values()),
                apiKey: $('#model-api-key').value,
            };
            const validation = validationService.validate(data, {
                rules: [
                    { field: 'providerId', validator: validationService.isSelected, message: 'Please select a provider.', mode: 'inline', target: '#provider-select' },
                    { field: 'selectedModels', validator: validationService.hasSelectedItems, message: 'Please select at least one model.', mode: 'inline', target: '#models-checkbox-list' },
                    { field: 'apiKey', validator: validationService.isNotEmpty, message: 'API key is required.', mode: 'inline', target: '#model-api-key' }
                ],
            });

            if (!validation.valid) {
                validationService.showErrors(validation.errors);
                return;
            }

            if (saveCallBack)
                saveCallBack(data);
        };
    },
    /**
     * Renders the provider dropdown and attaches selection logic.
     */
    renderProviders(providers, currentProviderId) {
        const select = $('#provider-select');
        if (!select) return;

        // Handle provider change to load respective models
        select.addEventListener('change', (e) => {
            const selectedId = e.target.value;
            const provider = providers.find(p => p.id === selectedId);

            // Reset state for the new provider
            this.pagination.goToPage(1);
            this.pagination.setItems((provider && provider.models) ? provider.models : []);
            $('#model-api-key').value = provider.apiKey;

            this.state.selectedModels = new Map(
                _.filter(provider.models, { isSelected: true })
                    .map(model => [model.id, model])
            );

            this.renderModelPage();

            if (provider && provider.models) {
                togglePanelHidden('#model-pagination', true);
            } else {
                togglePanelHidden('#model-pagination', false);
            }
        });

        select.innerHTML = '';
        addDefaultOption(select, 'Select an AI Provider');

        providers.forEach(p => {
            const option = document.createElement('option');
            option.value = p.id;
            option.textContent = p.name;

            select.appendChild(option);

            if (p.id === currentProviderId) {
                option.selected = true;
                trigger(select, 'change');
            }

        });

        if (!currentProviderId || currentProviderId == -1) {
            togglePanelHidden('#close-settings', false);
            select.selectedIndex = 0;
            this.show()
        }

    },

    /**
     * Renders a specific page of models based on the current state.
     */
    renderModelPage() {
        const listContainer = $('#models-checkbox-list');
        if (!listContainer) return;

        listContainer.innerHTML = '';

        const { selectedModels } = this.state;

        const pageItems = this.pagination.getPageItems();

        pageItems.forEach(model => {
            const item = document.createElement('div');
            item.className = 'model-item';

            const isChecked = selectedModels.has(model.id);

            item.innerHTML = `
                <input type="checkbox" id="model-${model.id}" value="${model.id}" ${isChecked ? 'checked' : ''}>
                <label for="model-${model.id}">
                    ${model.name} <span style="opacity:0.6; font-size:0.9em;">(Limit: ${model.tokenLimit ?? 'N/A'})</span>
                </label>
            `;

            // Row click listener to toggle checkbox
            item.addEventListener('click', (e) => {
                if (e.target.tagName !== 'DIV') return; // Prevent double trigger if clicking directly on checkbox

                const checkbox = item.querySelector('input[type="checkbox"]');
                if (!checkbox) return;

                const model = pageItems.find(p => p.id === checkbox.value);
                if (!model) return;

                if (selectedModels.has(model.id)) {
                    checkbox.checked = false;
                } else {
                    checkbox.checked = true;
                }

                this._handleCheckboxChange(checkbox.checked, model);
            });

            // Direct checkbox change listener
            const checkbox = item.querySelector('input[type="checkbox"]');
            checkbox.addEventListener('change', (e) => {
                this._handleCheckboxChange(e.target.checked, model);
            });

            listContainer.appendChild(item);
        });

        this._updatePaginationUI();
    },
    /**
     * @private
     */
    _updatePaginationUI() {
        const total = this.pagination.getTotalPages();
        const current = this.pagination.currentPage;

        $('#page-info').textContent = `Page ${current} of ${total}`;
        $('#prev-page').disabled = current === 1;
        $('#next-page').disabled = current === total;
    },
    /**
     * Helper to synchronize state with checkbox status.
     * @private
     */
    _handleCheckboxChange(checked, model) {
        if (checked) {
            this.state.selectedModels.set(model.id, model);
        } else {
            this.state.selectedModels.delete(model.id);
        }

    },

    /**
     * Shows the settings modal
     */
    show() {
        togglePanelHidden('#settings-modal', true);
    },

    /**
     * Hides the settings modal
     */
    hide() {
        togglePanelHidden('#settings-modal', false);
    },
};
