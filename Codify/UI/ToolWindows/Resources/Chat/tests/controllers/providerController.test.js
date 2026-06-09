/**
 * @file providerController.test.js
 * Unit tests for providerController logic.
 */

import { initProviderController } from '../../js/controllers/providerController.js';
import { setState } from '../../js/state/appState.js';
import { EVENTS } from '../../js/constants/events.js';

jest.mock('../../js/state/appState.js');
jest.mock('../../js/constants/events.js', () => ({
    EVENTS: {
        SELECT_PROVIDER: 'SELECT_PROVIDER'
    }
}));

describe('providerController', () => {
    let transport;
    let controller;

    beforeEach(() => {
        // Reset all mock calls before each test
        jest.clearAllMocks();

        // Mock transport used to communicate with the host backend
        transport = {
            send: jest.fn()
        };

        // Create controller instance
        controller = initProviderController(transport);
    });

    test('should update state and notify transport when provider changes', () => {
        // Act
        controller.changeProvider('ollama');

        // Assert
        expect(setState).toHaveBeenCalledWith({
            provider: 'ollama'
        });

        expect(transport.send).toHaveBeenCalledWith(
            EVENTS.SELECT_PROVIDER,
            { providerId: 'ollama' }
        );
    });

    test('should handle different provider ids correctly', () => {
        // Act
        controller.changeProvider('openai');

        // Assert
        expect(setState).toHaveBeenCalledWith({
            provider: 'openai'
        });

        expect(transport.send).toHaveBeenCalledWith(
            EVENTS.SELECT_PROVIDER,
            { providerId: 'openai' }
        );
    });
});
