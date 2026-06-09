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
        $('#settings-panel').classList.toggle('hidden', !visible);
    }
};
