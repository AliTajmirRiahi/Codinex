import { $ } from '../utils/dom.js';
import { EVENTS } from '../constants/events.js';
import { DropDownView } from '../views/dropDownView.js';
import { validationService } from '../services/validationService.js';
import { PaginationService } from '../services/paginationService.js';

export function initSettingsController(transport) {
    const settingsButton = $('#settings-btn');
    const settingsModal = $('#settings-modal');
    const closeButton = $('#close-settings-modal');
    const cancelButton = $('#cancel-settings-modal');
    const saveButton = $('#save-settings-modal');
    const autoAddActiveDocumentInput = $('#setting-auto-add-active-document');
    const enableStreamingChatInput = $('#setting-enable-streaming-chat');
    const enablePreprocessorAiInput = $('#setting-enable-preprocessor-ai');
    const preprocessorProviderSelect = $('#setting-preprocessor-provider');
    const preprocessorProviderButton = $('#setting-preprocessor-provider-selector-btn');
    const preprocessorProviderWrapper = preprocessorProviderButton?.closest('.provider-selector-wrapper');
    const preprocessorProviderName = $('#setting-preprocessor-provider-name');
    const preprocessorModelSelect = $('#setting-preprocessor-model');
    const preprocessorModelListContainer = $('#setting-preprocessor-model-list-container');
    const preprocessorModelsList = $('#setting-preprocessor-models-list');
    const preprocessorModelPagination = $('#setting-preprocessor-model-pagination');
    const preprocessorPrevPageButton = $('#setting-preprocessor-prev-page');
    const preprocessorNextPageButton = $('#setting-preprocessor-next-page');
    const preprocessorPageInfo = $('#setting-preprocessor-page-info');
    const tabs = Array.from(document.querySelectorAll('.settings-tab'));
    const panels = Array.from(document.querySelectorAll('.settings-tab-panel'));
    let currentSettings = {};
    let localProviders = [];
    let preprocessorProviderDropDown = null;
    const preprocessorModelPaginationService = new PaginationService([], 5);

    const getValue = (settings, camelCaseName, pascalCaseName, defaultValue = false) => {
        if (!settings) return defaultValue;
        if (settings[camelCaseName] !== undefined) return settings[camelCaseName];
        if (settings[pascalCaseName] !== undefined) return settings[pascalCaseName];
        return defaultValue;
    };

    const getProviderValue = (provider, camelCaseName, pascalCaseName, defaultValue = '') => {
        if (!provider) return defaultValue;
        if (provider[camelCaseName] !== undefined) return provider[camelCaseName];
        if (provider[pascalCaseName] !== undefined) return provider[pascalCaseName];
        return defaultValue;
    };

    const getModelValue = (model, camelCaseName, pascalCaseName, defaultValue = '') => {
        if (!model) return defaultValue;
        if (model[camelCaseName] !== undefined) return model[camelCaseName];
        if (model[pascalCaseName] !== undefined) return model[pascalCaseName];
        return defaultValue;
    };

    const getProviderList = (providersPayload) => {
        const providers = providersPayload?.availableProviders || providersPayload?.AvailableProviders || [];

        return providers
            .filter(provider => !!getProviderValue(provider, 'isLocal', 'IsLocal', false))
            .map(provider => ({
                ...provider,
                id: getProviderValue(provider, 'id', 'Id'),
                name: getProviderValue(provider, 'name', 'Name'),
                icon: getProviderValue(provider, 'icon', 'Icon', 'puzzle'),
                models: getProviderValue(provider, 'models', 'Models', []),
            }));
    };

    const getModelId = (model) => getModelValue(model, 'id', 'Id');

    const getModelName = (model) => getModelValue(model, 'name', 'Name', getModelId(model));

    const getModelTokenLimit = (model) => getModelValue(model, 'tokenLimit', 'TokenLimit', 'N/A');

    const getSelectedPreprocessorModelId = () => getValue(
        currentSettings,
        'preprocessorAiModelId',
        'PreprocessorAiModelId',
        '');

    const renderPreprocessorModelPage = () => {
        if (!preprocessorModelsList || !preprocessorModelSelect) return;

        preprocessorModelsList.innerHTML = '';

        const pageItems = preprocessorModelPaginationService.getPageItems();
        const selectedModelId = preprocessorModelSelect.value;

        if (pageItems.length === 0) {
            const emptyItem = document.createElement('div');
            emptyItem.className = 'checkbox-item';
            emptyItem.textContent = preprocessorProviderSelect?.value
                ? 'No models available for the selected local provider.'
                : 'Select a local provider to load models.';
            preprocessorModelsList.appendChild(emptyItem);
        }

        pageItems.forEach(model => {
            const modelId = getModelId(model);
            const modelName = getModelName(model);
            const tokenLimit = getModelTokenLimit(model);
            const item = document.createElement('div');
            item.className = 'model-item';

            const inputId = `setting-preprocessor-model-${modelId}`;
            const isChecked = modelId === selectedModelId;

            item.innerHTML = `
                <input type="radio" id="${inputId}" name="setting-preprocessor-model-option" value="${modelId}" ${isChecked ? 'checked' : ''}>
                <label for="${inputId}">
                    ${modelName} <span style="opacity:0.6; font-size:0.9em;">(Limit: ${tokenLimit ?? 'N/A'})</span>
                </label>
            `;

            const radio = item.querySelector('input[type="radio"]');

            item.addEventListener('click', (event) => {
                if (event.target.tagName === 'INPUT') return;

                radio.checked = true;
                preprocessorModelSelect.value = modelId;
                validationService.clearInlineError('#setting-preprocessor-models-list');
                renderPreprocessorModelPage();
            });

            radio.addEventListener('change', (event) => {
                if (!event.target.checked) return;

                preprocessorModelSelect.value = modelId;
                validationService.clearInlineError('#setting-preprocessor-models-list');
                renderPreprocessorModelPage();
            });

            preprocessorModelsList.appendChild(item);
        });

        const totalPages = preprocessorModelPaginationService.getTotalPages();
        const currentPage = preprocessorModelPaginationService.currentPage;

        if (preprocessorPageInfo) {
            preprocessorPageInfo.textContent = `Page ${currentPage} of ${totalPages}`;
        }

        if (preprocessorPrevPageButton) {
            preprocessorPrevPageButton.disabled = currentPage === 1;
        }

        if (preprocessorNextPageButton) {
            preprocessorNextPageButton.disabled = currentPage === totalPages;
        }

        preprocessorModelPagination?.classList.toggle('hidden', totalPages <= 1);
    };

    const renderPreprocessorModels = (provider, selectedModelId = '') => {
        if (!preprocessorModelSelect) return;

        const models = provider?.models || [];
        const modelExists = models.some(model => getModelId(model) === selectedModelId);

        preprocessorModelSelect.value = modelExists ? selectedModelId : '';
        preprocessorModelPaginationService.setItems(models);

        const isDisabled = !enablePreprocessorAiInput?.checked || !provider || models.length === 0;
        preprocessorModelListContainer?.classList.toggle('disable', isDisabled);

        renderPreprocessorModelPage();
    };

    const setSelectedPreprocessorProvider = (providerId, selectedModelId = getSelectedPreprocessorModelId()) => {
        if (!preprocessorProviderSelect) return;

        const selectedProvider = localProviders.find(provider => provider.id === providerId);

        preprocessorProviderSelect.value = selectedProvider ? selectedProvider.id : '';

        if (preprocessorProviderName) {
            preprocessorProviderName.textContent = selectedProvider
                ? selectedProvider.name
                : localProviders.length > 0
                    ? 'Select a local provider'
                    : 'No local providers available';
        }

        preprocessorProviderDropDown?.render(localProviders, preprocessorProviderSelect.value);
        renderPreprocessorModels(selectedProvider, selectedProvider ? selectedModelId : '');
    };

    const renderLocalProviders = () => {
        if (!preprocessorProviderSelect) return;

        const selectedProviderId = getValue(
            currentSettings,
            'preprocessorAiProviderId',
            'PreprocessorAiProviderId',
            '');

        if (!preprocessorProviderDropDown) {
            preprocessorProviderDropDown = new DropDownView({
                containerId: 'setting-preprocessor-provider-dropdown-menu-container',
                menuId: 'setting-preprocessor-provider-dropdown',
                menuButtonId: 'setting-preprocessor-provider-selector-btn',
                itemTemplate: (item, isActive) => {
                    const option = document.createElement('div');
                    option.className = `drop-option ${isActive ? 'active' : ''}`;
                    option.dataset.value = item.id;

                    option.innerHTML = `
                        <div class="drop-info">
                            <codinex-icon name="${item.icon || 'puzzle'}" class="provider-icon"></codinex-icon>
                            <span>${item.name}</span>
                        </div>`;
                    return option;
                },
                onItemSelect: (provider) => {
                    validationService.clearInlineError('#setting-preprocessor-provider-selector-btn');
                    setSelectedPreprocessorProvider(provider.id, '');
                    return true;
                }
            });
        }

        const isPreprocessorProviderDisabled = localProviders.length === 0 || !enablePreprocessorAiInput?.checked;
        preprocessorProviderButton.disabled = isPreprocessorProviderDisabled;
        preprocessorProviderButton.classList.toggle('disable', isPreprocessorProviderDisabled);
        preprocessorProviderWrapper?.classList.toggle('disable', isPreprocessorProviderDisabled);
        preprocessorModelsList?.classList.toggle('disable', isPreprocessorProviderDisabled);

        if (isPreprocessorProviderDisabled) {
            preprocessorProviderDropDown.hide();
        }

        if (isPreprocessorProviderDisabled && preprocessorModelSelect) {
            preprocessorModelSelect.value = '';
        }

        preprocessorProviderDropDown.render(localProviders, selectedProviderId);
        setSelectedPreprocessorProvider(selectedProviderId);
    };

    const applySettingsToForm = () => {
        if (autoAddActiveDocumentInput) {
            autoAddActiveDocumentInput.checked = !!getValue(
                currentSettings,
                'autoAddActiveDocumentToMessage',
                'AutoAddActiveDocumentToMessage');
        }

        if (enableStreamingChatInput) {
            enableStreamingChatInput.checked = !!getValue(
                currentSettings,
                'enableStreamingChat',
                'EnableStreamingChat',
                true);
        }

        if (enablePreprocessorAiInput) {
            enablePreprocessorAiInput.checked = !!getValue(
                currentSettings,
                'enablePreprocessorAi',
                'EnablePreprocessorAi');
        }

        renderLocalProviders();
    };

    const openSettingsModal = () => {
        applySettingsToForm();
        settingsModal?.classList.remove('hidden');
    };

    const closeSettingsModal = () => {
        settingsModal?.classList.add('hidden');
    };

    const selectTab = (tabName) => {
        tabs.forEach(tab => {
            const isActive = tab.dataset.settingsTab === tabName;

            tab.classList.toggle('active', isActive);
            tab.setAttribute('aria-selected', isActive ? 'true' : 'false');
        });

        panels.forEach(panel => {
            panel.classList.toggle('active', panel.dataset.settingsPanel === tabName);
        });
    };

    settingsButton?.addEventListener('click', openSettingsModal);
    closeButton?.addEventListener('click', closeSettingsModal);
    cancelButton?.addEventListener('click', closeSettingsModal);
    preprocessorPrevPageButton?.addEventListener('click', () => {
        if (preprocessorModelPaginationService.currentPage <= 1) return;

        preprocessorModelPaginationService.prevPage();
        renderPreprocessorModelPage();
    });
    preprocessorNextPageButton?.addEventListener('click', () => {
        if (preprocessorModelPaginationService.currentPage >= preprocessorModelPaginationService.getTotalPages()) return;

        preprocessorModelPaginationService.nextPage();
        renderPreprocessorModelPage();
    });
    saveButton?.addEventListener('click', () => {
        const data = {
            enablePreprocessorAi: !!enablePreprocessorAiInput?.checked,
            preprocessorAiProviderId: preprocessorProviderSelect?.value || '',
            preprocessorAiModelId: preprocessorModelSelect?.value || '',
        };

        validationService.clearInlineError('#setting-preprocessor-provider-selector-btn');
        validationService.clearInlineError('#setting-preprocessor-models-list');

        const validation = validationService.validate(data, {
            rules: [
                {
                    field: 'preprocessorAiProviderId',
                    validator: (value, formData) => !formData.enablePreprocessorAi || validationService.isSelected(value),
                    message: 'Please select a local provider.',
                    mode: 'inline',
                    target: '#setting-preprocessor-provider-selector-btn',
                },
                {
                    field: 'preprocessorAiModelId',
                    validator: (value, formData) => !formData.enablePreprocessorAi || validationService.isSelected(value),
                    message: 'Please select an AI model.',
                    mode: 'inline',
                    target: '#setting-preprocessor-models-list',
                },
            ],
        });

        if (!validation.valid) {
            const firstError = validation.errors[0];

            if (firstError?.field === 'preprocessorAiProviderId' || firstError?.field === 'preprocessorAiModelId') {
                selectTab('prompt-preprocessor');
            }

            validationService.showErrors(validation.errors);
            return;
        }

        currentSettings = {
            ...currentSettings,
            autoAddActiveDocumentToMessage: !!autoAddActiveDocumentInput?.checked,
            enableStreamingChat: !!enableStreamingChatInput?.checked,
            enablePreprocessorAi: data.enablePreprocessorAi,
            preprocessorAiProviderId: data.preprocessorAiProviderId,
            preprocessorAiModelId: data.preprocessorAiModelId,
        };

        transport?.send(EVENTS.SAVE_SETTINGS, currentSettings);
    });

    enablePreprocessorAiInput?.addEventListener('change', renderLocalProviders);

    tabs.forEach(tab => {
        tab.addEventListener('click', () => {
            selectTab(tab.dataset.settingsTab);
        });
    });

    settingsModal?.addEventListener('click', (event) => {
        if (event.target === settingsModal) {
            closeSettingsModal();
        }
    });

    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape' && settingsModal && !settingsModal.classList.contains('hidden')) {
            closeSettingsModal();
        }
    });

    return {
        updateUI(settings, providers) {
            currentSettings = settings || {};

            if (providers) {
                localProviders = getProviderList(providers);
            }

            applySettingsToForm();
        },
        closeSettingsModal,
    };
}
