/**
 * @file appState.test.js
 * Unit tests for appState.js
 */

import { getState, setState, addMessage } from '../../js/state/appState.js';

describe('appState', () => {
    beforeEach(() => {
        // Reset the singleton state before each test to avoid state leakage
        setState({
            provider: null,
            model: null,
            messages: [],
            isLoading: false
        });
    });

    test('getState should return the shared state object', () => {
        // Act
        const state = getState();

        // Assert
        expect(state).toEqual({
            provider: null,
            model: null,
            messages: [],
            isLoading: false
        });
    });

    test('setState should merge partial state into the shared state', () => {
        // Act
        setState({
            provider: 'openai',
            isLoading: true
        });

        // Assert
        const state = getState();
        expect(state.provider).toBe('openai');
        expect(state.model).toBeNull();
        expect(state.messages).toEqual([]);
        expect(state.isLoading).toBe(true);
    });

    test('addMessage should append a message to the messages array', () => {
        // Arrange
        const message = {
            role: 'user',
            content: 'Hello'
        };

        // Act
        addMessage(message);

        // Assert
        const state = getState();
        expect(state.messages).toHaveLength(1);
        expect(state.messages[0]).toEqual(message);
    });

    test('addMessage should preserve existing messages', () => {
        // Arrange
        const firstMessage = { role: 'user', content: 'First' };
        const secondMessage = { role: 'assistant', content: 'Second' };

        // Act
        addMessage(firstMessage);
        addMessage(secondMessage);

        // Assert
        const state = getState();
        expect(state.messages).toHaveLength(2);
        expect(state.messages).toEqual([firstMessage, secondMessage]);
    });
});
