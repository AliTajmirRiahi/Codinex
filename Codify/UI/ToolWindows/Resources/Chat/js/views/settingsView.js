import { $ } from '../utils/dom.js';

/**
 * Manages the settings panel UI.
 */
export const settingsView = {
    renderProviders(providers, currentProvider) {
        const select = $('#provider-select');
        select.innerHTML = providers.map(p =>
            `<option value="${p.id}" ${p.id === currentProvider ? 'selected' : ''}>${p.name}</option>`
        ).join('');
    },

    toggleSettingsPanel(visible) {
        $('#settings-modal').classList.toggle('hidden', !visible);
    },

    /**
     * Shows the settings modal
     */
    show() {
        const modal = document.getElementById('settings-modal');
        if (modal) {
            this.toggleSettingsPanel(true);
        }
    },

    /**
     * Hides the settings modal
     */
    hide() {
        const modal = document.getElementById('settings-modal');
        if (modal) {
            this.toggleSettingsPanel(false);
        }
    },
};
