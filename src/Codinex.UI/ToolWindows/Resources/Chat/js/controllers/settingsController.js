import { $ } from '../utils/dom.js';
import { EVENTS } from '../constants/events.js';

export function initSettingsController(transport) {
    const settingsButton = $('#settings-btn');
    const settingsModal = $('#settings-modal');
    const closeButton = $('#close-settings-modal');
    const cancelButton = $('#cancel-settings-modal');
    const saveButton = $('#save-settings-modal');
    const autoAddActiveDocumentInput = $('#setting-auto-add-active-document');
    const enableStreamingChatInput = $('#setting-enable-streaming-chat');
    const tabs = Array.from(document.querySelectorAll('.settings-tab'));
    const panels = Array.from(document.querySelectorAll('.settings-tab-panel'));
    let currentSettings = {};

    const getValue = (settings, camelCaseName, pascalCaseName, defaultValue = false) => {
        if (!settings) return defaultValue;
        if (settings[camelCaseName] !== undefined) return settings[camelCaseName];
        if (settings[pascalCaseName] !== undefined) return settings[pascalCaseName];
        return defaultValue;
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
        updateUI(settings) {
            currentSettings = settings || {};
            applySettingsToForm();
        },
        closeSettingsModal,
    };
}
