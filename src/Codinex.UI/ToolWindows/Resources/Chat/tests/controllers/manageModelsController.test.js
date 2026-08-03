/**
 * @file settingsController.test.js
 * Unit tests for settingsController
 */

import { initSettingsController } from '../../js/controllers/settingsController.js';
import { settingsView } from '../../js/views/settingsView.js';

jest.mock('../../js/views/settingsView.js', () => ({
    settingsView: {
        renderProviders: jest.fn()
    }
}));

describe('settingsController', () => {

    beforeEach(() => {
        document.body.innerHTML = '';
        jest.clearAllMocks();
    });

    test('should send UPDATE_SETTINGS when provider changes', () => {
        document.body.innerHTML = `
            <select id="provider-select">
                <option value="openai">OpenAI</option>
                <option value="ollama">Ollama</option>
            </select>
        `;

        const transport = {
            send: jest.fn()
        };

        initSettingsController(transport);

        const select = document.getElementById('provider-select');
        select.value = 'ollama';

        select.dispatchEvent(new Event('change'));

        expect(transport.send).toHaveBeenCalledWith(
            'UPDATE_SETTINGS',
            { provider: 'ollama' }
        );
    });

    test('updateUI should render providers via settingsView', () => {
        const transport = { send: jest.fn() };

        const controller = initSettingsController(transport);

        const settings = {
            availableProviders: ['openai', 'ollama'],
            current: 'openai'
        };

        controller.updateUI(settings);

        expect(settingsView.renderProviders).toHaveBeenCalledWith(
            settings.availableProviders,
            settings.current
        );
    });

});
