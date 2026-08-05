import { $ } from '../utils/dom.js';
import { EVENTS } from '../constants/events.js';
import { DropDownView } from '../views/dropDownView.js';

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
    const preprocessorProviderName = $('#setting-preprocessor-provider-name');
    const tabs = Array.from(document.querySelectorAll('.settings-tab'));
    const panels = Array.from(document.querySelectorAll('.settings-tab-panel'));
    let currentSettings = {};
    let localProviders = [];
    let preprocessorProviderDropDown = null;

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

    const getProviderList = (providersPayload) => {
        const providers = providersPayload?.availableProviders || providersPayload?.AvailableProviders || [];

        return providers
            .filter(provider => !!getProviderValue(provider, 'isLocal', 'IsLocal', false))
            .map(provider => ({
                ...provider,
                id: getProviderValue(provider, 'id', 'Id'),
                name: getProviderValue(provider, 'name', 'Name'),
                icon: getProviderValue(provider, 'icon', 'Icon', 'puzzle'),
            }));
    };

    const setSelectedPreprocessorProvider = (providerId) => {
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
                    setSelectedPreprocessorProvider(provider.id);
                    return true;
                }
            });
        }

        preprocessorProviderButton.disabled = localProviders.length === 0;
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
    saveButton?.addEventListener('click', () => {
        currentSettings = {
            ...currentSettings,
            autoAddActiveDocumentToMessage: !!autoAddActiveDocumentInput?.checked,
            enableStreamingChat: !!enableStreamingChatInput?.checked,
            enablePreprocessorAi: !!enablePreprocessorAiInput?.checked,
            preprocessorAiProviderId: preprocessorProviderSelect?.value || '',
        };

        transport?.send(EVENTS.SAVE_SETTINGS, currentSettings);
    });

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
