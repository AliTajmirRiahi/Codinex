/* 
 * path: Codinex\UI\ToolWindows\Resources\Chat\js\views\manageModelsView.js
 */
import { $, togglePanelHidden } from '../utils/dom.js';
import { PaginationService } from '../services/paginationService.js';
import { validationService } from '../services/validationService.js';
import { DropDownView } from '../views/dropDownView.js';

/**
 * Manages the settings panel UI, including provider selection, 
 * model pagination, and state persistence for selections.
 */
export const manageModelsView = {
    // Internal state to manage models across pages
    state: {
        selectedModels: new Map(), // Stores full model objects to maintain 
        providers: [],
        allModels: [],
        modelSearchTerm: '',
        currentProviderId: '',
    },
    // We now store a pagination instance instead of raw pagination values
    pagination: new PaginationService([], 5),

    initEventHandlers(saveCallBack, refreshModelsCallBack) {
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

        const modelSearchInput = $('#model-search-input');
        if (modelSearchInput) {
            modelSearchInput.addEventListener('input', (e) => {
                this.state.modelSearchTerm = e.target.value || '';
                this.pagination.goToPage(1);
                this._applyModelFilter();
                this.renderModelPage();
            });
        }

        $('#refresh-models-btn').onclick = () => {
            const provider = this.getSelectedProvider();

            if (!provider) return;

            const needsApiKey = this._providerNeedsApiKey(provider);
            if (needsApiKey && !provider.apiKey) return;

            // Show loading overlay while refreshing provider models
            this.setLoading(true);

            if (refreshModelsCallBack)
                refreshModelsCallBack({ providerId: provider.id });
        };

        $('#save-settings-btn').onclick = () => {
            const data = {
                providerId: $('#provider-select').value,
                selectedModels: Array.from(this.state.selectedModels.values()),
                apiKey: $('#model-api-key').value,
            };
            const provider = this.getSelectedProvider();
            const rules = [
                { field: 'providerId', validator: validationService.isSelected, message: 'Please select a provider.', mode: 'inline', target: '#provider-selector-btn' },
                { field: 'selectedModels', validator: validationService.hasSelectedItems, message: 'Please select at least one model.', mode: 'inline', target: '#models-checkbox-list' },
            ];

            if (this._providerNeedsApiKey(provider)) {
                rules.push({ field: 'apiKey', validator: validationService.isNotEmpty, message: 'API key is required.', mode: 'inline', target: '#model-api-key' });
            }

            const validation = validationService.validate(data, {
                rules,
            });

            if (!validation.valid) {
                validationService.showErrors(validation.errors);
                return;
            }

            this.setLoading(true);

            if (saveCallBack)
                saveCallBack(data);
        };
    },
    setLoading(isLoading) {
        togglePanelHidden('#models-loading-screen', !!isLoading);
    },
    showError(message) {
        this.setLoading(false);

        validationService.showError({
            message: message || 'Settings could not be saved.',
            mode: 'inline',
            target: '#models-checkbox-list'
        });
    },
    /**
     * Renders the provider dropdown and attaches selection logic.
     */
    renderProviders(providers, currentProviderId) {
        const select = $('#provider-select');
        if (!select) return;

        this.state.providers = providers || [];

        if (!this.providerDropDown) {
            this.providerDropDown = new DropDownView({
                containerId: 'provider-dropdown-menu-container',
                menuId: 'provider-dropdown',
                menuButtonId: 'provider-selector-btn',
                itemTemplate: (item, isActive) => {
                    const option = document.createElement('div');
                    option.className = `drop-option ${isActive ? 'active' : ''}`;
                    option.dataset.value = item.id;

                    option.innerHTML = `
                        <div class="drop-info">
                            <codinex-icon name="${item.icon || 'puzzle'}" class="provider-icon" style="color: ${item.iconColor || item.IconColor || '#000000'};"></codinex-icon>
                            <span>${item.name}</span>
                        </div>`;
                    return option;
                },
                onItemSelect: (provider) => {
                    this.setSelectedProvider(provider.id);
                    return true;
                }
            });
        }

        select.onchange = (e) => {
            const selectedId = e.target.value;
            const provider = this.state.providers.find(p => p.id === selectedId);

            this.renderProviderModels(provider);

            if (provider && provider.models) {
                togglePanelHidden('#model-pagination', true);
            } else {
                togglePanelHidden('#model-pagination', false);
            }
        };

        const selectedProvider = this.state.providers.find(p => p.id === currentProviderId);
        this.providerDropDown.render(this.state.providers, selectedProvider ? selectedProvider.id : '');

        if (selectedProvider) {
            this.setSelectedProvider(selectedProvider.id);
        } else {
            select.value = '';
            this.state.currentProviderId = '';
            this.setCurrentProviderName();
        }

        if (!currentProviderId || currentProviderId == -1) {
            togglePanelHidden('#close-settings', false);
            this.show()
        }

    },

    setSelectedProvider(providerId) {
        const select = $('#provider-select');
        if (!select) return;

        select.value = providerId || '';
        this.state.currentProviderId = select.value;
        this.setCurrentProviderName();
        select.dispatchEvent(new Event('change', { bubbles: true }));
    },

    setCurrentProviderName() {
        const providerName = $('#provider-name');
        if (!providerName) return;

        const provider = this.state.providers.find(p => p.id === this.state.currentProviderId);
        providerName.textContent = provider ? provider.name : 'Select an AI Provider';
    },

    renderProviderModels(provider) {
        // Reset state for the selected provider
        this.state.allModels = (provider && provider.models) ? provider.models : [];
        this.state.modelSearchTerm = '';
        const modelSearchInput = $('#model-search-input');
        if (modelSearchInput) modelSearchInput.value = '';

        this.pagination.goToPage(1);
        this._applyModelFilter();
        $('#model-api-key').value = provider ? provider.apiKey : '';
        const needsApiKey = this._providerNeedsApiKey(provider);
        togglePanelHidden('#refresh-models-btn', !!(provider && (!needsApiKey || provider.apiKey)));

        this.state.selectedModels = new Map(
            _.filter(this.state.allModels, { isSelected: true })
                .map(model => [model.id, model])
        );

        this.renderModelPage();
    },

    getSelectedProvider() {
        const providerId = $('#provider-select').value;

        return this.state.providers.find(p => p.id === providerId);
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

        if (pageItems.length === 0) {
            const emptyItem = document.createElement('div');
            emptyItem.className = 'model-item empty-model-item';
            emptyItem.textContent = this.state.modelSearchTerm ? 'No models found.' : 'No models available.';
            listContainer.appendChild(emptyItem);
            this._updatePaginationUI();
            return;
        }

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
        const paginationVisible = this.pagination.items.length > this.pagination.itemsPerPage;

        $('#page-info').textContent = `Page ${current} of ${total}`;
        $('#prev-page').disabled = current === 1;
        $('#next-page').disabled = current === total;
        togglePanelHidden('#model-pagination', paginationVisible);
    },

    _applyModelFilter() {
        const searchTerm = (this.state.modelSearchTerm || '').trim().toLowerCase();
        const filteredModels = searchTerm
            ? this.state.allModels.filter(model => (model.name || '').toLowerCase().includes(searchTerm))
            : this.state.allModels;

        this.pagination.setItems(filteredModels);
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
     * @private
     */
    _providerNeedsApiKey(provider) {
        if (!provider) return true;

        return provider.needApiKey !== false;
    },

    /**
     * Shows the settings modal
     */
    show() {
        togglePanelHidden('#model-management-modal', true);
    },

    /**
     * Hides the settings modal
     */
    hide() {
        togglePanelHidden('#model-management-modal', false);
    },
};
