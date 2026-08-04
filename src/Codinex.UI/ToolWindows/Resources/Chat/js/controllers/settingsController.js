import { $ } from '../utils/dom.js';

export function initSettingsController() {
    const settingsButton = $('#settings-btn');
    const settingsModal = $('#settings-modal');
    const closeButton = $('#close-settings-modal');
    const cancelButton = $('#cancel-settings-modal');
    const saveButton = $('#save-settings-modal');
    const tabs = Array.from(document.querySelectorAll('.settings-tab'));
    const panels = Array.from(document.querySelectorAll('.settings-tab-panel'));

    const openSettingsModal = () => {
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
    saveButton?.addEventListener('click', closeSettingsModal);

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
}
