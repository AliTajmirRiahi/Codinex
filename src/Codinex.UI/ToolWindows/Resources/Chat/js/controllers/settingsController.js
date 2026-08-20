import { $ } from '../utils/dom.js';
import { EVENTS } from '../constants/events.js';
import { DropDownView } from '../views/dropDownView.js';
import { validationService } from '../services/validationService.js';
import { PaginationService } from '../services/paginationService.js';
import { getState } from '../state/appState.js';

export function initSettingsController(transport) {
    const settingsButton = $('#settings-btn');
    const settingsDropdownMenu = $('#settings-dropdown-menu');
    const openCodinexSettingsAction = $('#open-codinex-settings-action');
    const openSolutionSettingsAction = $('#open-solution-settings-action');
    const solutionSettingsMenuLabel = $('#solution-settings-menu-label');

    const codinexSettingsModal = $('#codinex-settings-modal');
    const closeCodinexSettingsButton = $('#close-codinex-settings-modal');
    const cancelCodinexSettingsButton = $('#cancel-codinex-settings-modal');
    const saveCodinexSettingsButton = $('#save-codinex-settings-modal');

    const solutionSettingsModal = $('#solution-settings-modal');
    const solutionSettingsModalTitle = $('#solution-settings-modal-title');
    const closeSolutionSettingsButton = $('#close-solution-settings-modal');
    const cancelSolutionSettingsButton = $('#cancel-solution-settings-modal');
    const saveSolutionSettingsButton = $('#save-solution-settings-modal');

    const autoAddActiveDocumentInput = $('#setting-auto-add-active-document');
    const enableStreamingChatInput = $('#setting-enable-streaming-chat');
    const bypassPreviewChangeInput = $('#setting-bypass-preview-change');
    const enablePreprocessorAiInput = $('#setting-enable-preprocessor-ai');
    const solutionInstructionInput = $('#setting-solution-instruction');
    const excludeDirectoriesInput = $('#setting-exclude-directories');
    const excludeFilesInput = $('#setting-exclude-files');
    const ignoredExtensionsInput = $('#setting-ignored-extensions');
    const ignoredFileSuffixesInput = $('#setting-ignored-file-suffixes');
    const commitMessagePromptInput = $('#setting-commit-message-prompt');
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
    // Scoped to the solution-settings modal — the Codinex-settings modal has its own
    // single, always-active panel and no tabs of its own.
    const tabs = Array.from(solutionSettingsModal?.querySelectorAll('.settings-tab') || []);
    const panels = Array.from(solutionSettingsModal?.querySelectorAll('.settings-tab-panel') || []);
    let currentSettings = {};
    let currentWorkspaceSettings = {};
    let canBypassPreviewChange = false;
    let localProviders = [];
    let preprocessorProviderDropDown = null;
    const preprocessorModelPaginationService = new PaginationService([], 5);

    const getValue = (settings, camelCaseName, pascalCaseName, defaultValue = false) => {
        if (!settings) return defaultValue;
        if (settings[camelCaseName] !== undefined) return settings[camelCaseName];
        if (settings[pascalCaseName] !== undefined) return settings[pascalCaseName];
        return defaultValue;
    };

    const getCommitMessagePromptValue = (settings) => {
        if (!settings) return '';
        if (settings.commitMessageSystemPrompt !== undefined) return settings.commitMessageSystemPrompt;
        if (settings.CommitMessageSystemPrompt !== undefined) return settings.CommitMessageSystemPrompt;
        return '';
    };

    const getSourceControlValue = (sourceControl) => {
        if (!sourceControl) return false;
        return !!(sourceControl.isSolutionUnderSourceControl ?? sourceControl.IsSolutionUnderSourceControl ?? false);
    };

    const updateBypassPreviewAvailability = () => {
        if (!bypassPreviewChangeInput) return;

        const option = bypassPreviewChangeInput.closest('.setting-checkbox-option');
        bypassPreviewChangeInput.disabled = !canBypassPreviewChange;
        option?.classList.toggle('disable', !canBypassPreviewChange);
        option?.setAttribute(
            'title',
            canBypassPreviewChange
                ? 'Apply workspace changes directly without preview.'
                : 'Available only when the solution is under source control such as Git or TFS.');

        if (!canBypassPreviewChange) {
            bypassPreviewChangeInput.checked = false;
        }
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

        if (bypassPreviewChangeInput) {
            bypassPreviewChangeInput.checked = canBypassPreviewChange && !!getValue(
                currentSettings,
                'byPassPreviewChangeAndApplyChangeDirectly',
                'ByPassPreviewChangeAndApplyChangeDirectly');

            updateBypassPreviewAvailability();
        }

        if (enablePreprocessorAiInput) {
            enablePreprocessorAiInput.checked = !!getValue(
                currentSettings,
                'enablePreprocessorAi',
                'EnablePreprocessorAi');
        }

        renderLocalProviders();

        if (solutionInstructionInput) {
            solutionInstructionInput.value = getValue(
                currentWorkspaceSettings,
                'solutionInstruction',
                'SolutionInstruction',
                '');
        }

        if (excludeDirectoriesInput) {
            excludeDirectoriesInput.value = getValue(
                currentWorkspaceSettings,
                'excludeDirectories',
                'ExcludeDirectories',
                '');
        }

        if (excludeFilesInput) {
            excludeFilesInput.value = getValue(
                currentWorkspaceSettings,
                'excludeFiles',
                'ExcludeFiles',
                '');
        }

        if (ignoredExtensionsInput) {
            ignoredExtensionsInput.value = getValue(
                currentWorkspaceSettings,
                'ignoredExtensions',
                'IgnoredExtensions',
                '');
        }

        if (ignoredFileSuffixesInput) {
            ignoredFileSuffixesInput.value = getValue(
                currentWorkspaceSettings,
                'ignoredFileSuffixes',
                'IgnoredFileSuffixes',
                '');
        }

        if (commitMessagePromptInput) {
            commitMessagePromptInput.value = getCommitMessagePromptValue(currentSettings);
        }
    };

    const updateSolutionSettingsLabel = () => {
        const solutionName = getState().solutionName;
        const label = solutionName ? `${solutionName} Settings` : 'Solution Settings';

        if (solutionSettingsMenuLabel) solutionSettingsMenuLabel.textContent = label;
        if (solutionSettingsModalTitle) solutionSettingsModalTitle.textContent = label;
    };

    let isSettingsMenuOpen = false;

    const closeSettingsMenu = () => {
        isSettingsMenuOpen = false;
        settingsDropdownMenu?.classList.remove('show');
    };

    const toggleSettingsMenu = (event) => {
        event.stopPropagation();
        window.dispatchEvent(new CustomEvent('ui:close-all-dropdowns'));
        updateSolutionSettingsLabel();
        isSettingsMenuOpen = !isSettingsMenuOpen;
        settingsDropdownMenu?.classList.toggle('show', isSettingsMenuOpen);
    };

    const openCodinexSettingsModal = () => {
        applySettingsToForm();
        codinexSettingsModal?.classList.remove('hidden');
    };

    const closeCodinexSettingsModal = () => {
        codinexSettingsModal?.classList.add('hidden');
    };

    const openSolutionSettingsModal = () => {
        updateSolutionSettingsLabel();
        applySettingsToForm();
        solutionSettingsModal?.classList.remove('hidden');
    };

    const closeSolutionSettingsModal = () => {
        solutionSettingsModal?.classList.add('hidden');
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

    settingsButton?.addEventListener('click', toggleSettingsMenu);
    openCodinexSettingsAction?.addEventListener('click', () => {
        closeSettingsMenu();
        openCodinexSettingsModal();
    });
    openSolutionSettingsAction?.addEventListener('click', () => {
        closeSettingsMenu();
        openSolutionSettingsModal();
    });
    document.addEventListener('click', (event) => {
        if (isSettingsMenuOpen
            && !settingsDropdownMenu?.contains(event.target)
            && !settingsButton?.contains(event.target)) {
            closeSettingsMenu();
        }
    });
    window.addEventListener('ui:close-all-dropdowns', closeSettingsMenu);

    closeCodinexSettingsButton?.addEventListener('click', closeCodinexSettingsModal);
    cancelCodinexSettingsButton?.addEventListener('click', closeCodinexSettingsModal);

    closeSolutionSettingsButton?.addEventListener('click', closeSolutionSettingsModal);
    cancelSolutionSettingsButton?.addEventListener('click', closeSolutionSettingsModal);

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

    // Codinex Settings — global, app-level behavior. Sends the full settings object
    // (SaveSettingsAsync replaces it wholesale) so fields owned by the Solution Settings
    // modal are preserved as-is.
    saveCodinexSettingsButton?.addEventListener('click', () => {
        currentSettings = {
            ...currentSettings,
            autoAddActiveDocumentToMessage: !!autoAddActiveDocumentInput?.checked,
            enableStreamingChat: !!enableStreamingChatInput?.checked,
            byPassPreviewChangeAndApplyChangeDirectly: !!bypassPreviewChangeInput?.checked && canBypassPreviewChange,
        };

        transport?.send(EVENTS.SAVE_SETTINGS, currentSettings);
    });

    // Solution Settings — provider/model preprocessing config, the commit message prompt,
    // and the per-solution workspace settings (instruction/exclusions).
    saveSolutionSettingsButton?.addEventListener('click', () => {
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
            enablePreprocessorAi: data.enablePreprocessorAi,
            preprocessorAiProviderId: data.preprocessorAiProviderId,
            preprocessorAiModelId: data.preprocessorAiModelId,
            commitMessageSystemPrompt: commitMessagePromptInput?.value || '',
        };

        transport?.send(EVENTS.SAVE_SETTINGS, currentSettings);

        currentWorkspaceSettings = {
            ...currentWorkspaceSettings,
            solutionInstruction: solutionInstructionInput?.value || '',
            excludeDirectories: excludeDirectoriesInput?.value || '',
            excludeFiles: excludeFilesInput?.value || '',
            ignoredExtensions: ignoredExtensionsInput?.value || '',
            ignoredFileSuffixes: ignoredFileSuffixesInput?.value || '',
        };

        transport?.send(EVENTS.SAVE_SOLUTION_INSTRUCTION, {
            solutionInstruction: currentWorkspaceSettings.solutionInstruction,
            excludeDirectories: currentWorkspaceSettings.excludeDirectories,
            excludeFiles: currentWorkspaceSettings.excludeFiles,
            ignoredExtensions: currentWorkspaceSettings.ignoredExtensions,
            ignoredFileSuffixes: currentWorkspaceSettings.ignoredFileSuffixes,
        });
    });

    enablePreprocessorAiInput?.addEventListener('change', renderLocalProviders);

    tabs.forEach(tab => {
        tab.addEventListener('click', () => {
            selectTab(tab.dataset.settingsTab);
        });
    });

    codinexSettingsModal?.addEventListener('click', (event) => {
        if (event.target === codinexSettingsModal) {
            closeCodinexSettingsModal();
        }
    });

    solutionSettingsModal?.addEventListener('click', (event) => {
        if (event.target === solutionSettingsModal) {
            closeSolutionSettingsModal();
        }
    });

    document.addEventListener('keydown', (event) => {
        if (event.key !== 'Escape') return;

        if (codinexSettingsModal && !codinexSettingsModal.classList.contains('hidden')) {
            closeCodinexSettingsModal();
        }

        if (solutionSettingsModal && !solutionSettingsModal.classList.contains('hidden')) {
            closeSolutionSettingsModal();
        }
    });

    return {
        updateUI(settings, providers, workspaceSettings, sourceControl) {
            if (sourceControl) {
                canBypassPreviewChange = getSourceControlValue(sourceControl);
            }

            if (settings) {
                currentSettings = settings;
            }

            if (providers) {
                localProviders = getProviderList(providers);
            }

            if (workspaceSettings) {
                currentWorkspaceSettings = workspaceSettings;
            }

            applySettingsToForm();
        },
        closeSettingsModal() {
            closeCodinexSettingsModal();
            closeSolutionSettingsModal();
        },
    };
}
