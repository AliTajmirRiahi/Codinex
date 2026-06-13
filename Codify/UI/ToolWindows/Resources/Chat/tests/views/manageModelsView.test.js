/**
 * @file settingsView.test.js
 * Unit tests for settingsView
 */

import { settingsView } from '../../js/views/settingsView.js';

// Mocking the $ helper from dom.js
jest.mock('../../js/utils/dom.js', () => ({
    $: jest.fn()
}));

import { $ } from '../../js/utils/dom.js';

describe('settingsView', () => {
    let mockSelect;
    let mockPanel;

    beforeEach(() => {
        // Setup mock elements
        mockSelect = { innerHTML: '' };
        mockPanel = { classList: { toggle: jest.fn() } };

        // Setup the $ mock to return correct elements
        $.mockImplementation((selector) => {
            if (selector === '#provider-select') return mockSelect;
            if (selector === '#settings-panel') return mockPanel;
            return null;
        });
    });

    test('renderProviders should generate correct option elements', () => {
        const providers = [
            { id: 'openai', name: 'OpenAI' },
            { id: 'ollama', name: 'Ollama (Local)' }
        ];
        const currentProvider = 'ollama';

        settingsView.renderProviders(providers, currentProvider);

        // Check if the HTML is generated correctly
        expect(mockSelect.innerHTML).toContain('value="openai"');
        expect(mockSelect.innerHTML).toContain('value="ollama"');

        // Verify that 'ollama' is marked as selected
        expect(mockSelect.innerHTML).toContain('<option value="ollama" selected>Ollama (Local)</option>');
        // Verify that 'openai' is NOT selected
        expect(mockSelect.innerHTML).not.toContain('<option value="openai" selected>');
    });

    test('toggleSettingsPanel should toggle hidden class correctly', () => {
        // Test opening the panel (visible = true)
        settingsView.toggleSettingsPanel(true);
        expect(mockPanel.classList.toggle).toHaveBeenCalledWith('hidden', false);

        // Test closing the panel (visible = false)
        settingsView.toggleSettingsPanel(false);
        expect(mockPanel.classList.toggle).toHaveBeenCalledWith('hidden', true);
    });
});
