/**
 * @file aiService.test.js
 * Unit tests for aiService.js
 */

import { aiService } from '../../js/services/aiService.js';
import { EVENTS } from '../../js/constants/events.js';

describe('aiService', () => {
    describe('sendMessage', () => {
        afterEach(() => {
            jest.restoreAllMocks();
        });

        test('should call transport.send with SEND_MESSAGE event and correct payload', () => {
            // Arrange
            const transport = {
                send: jest.fn()
            };

            const prompt = 'Hello AI';
            const fixedTimestamp = '2026-01-01T12:00:00.000Z';

            // Mock the timestamp to make the test deterministic
            jest.spyOn(Date.prototype, 'toISOString').mockReturnValue(fixedTimestamp);

            // Act
            aiService.sendMessage(prompt, transport);

            // Assert
            expect(transport.send).toHaveBeenCalledTimes(1);
            expect(transport.send).toHaveBeenCalledWith(
                EVENTS.SEND_MESSAGE,
                {
                    content: prompt,
                    timestamp: fixedTimestamp
                }
            );
        });
    });

    describe('processStreamChunk', () => {
        test('should append chunk to current message when payload.chunk exists', () => {
            // Arrange
            const state = {
                appendToCurrentMessage: jest.fn()
            };

            const payload = {
                chunk: 'Hello chunk'
            };

            // Act
            aiService.processStreamChunk(payload, state);

            // Assert
            expect(state.appendToCurrentMessage).toHaveBeenCalledTimes(1);
            expect(state.appendToCurrentMessage).toHaveBeenCalledWith('Hello chunk');
        });

        test('should do nothing when payload is null', () => {
            // Arrange
            const state = {
                appendToCurrentMessage: jest.fn()
            };

            // Act
            aiService.processStreamChunk(null, state);

            // Assert
            expect(state.appendToCurrentMessage).not.toHaveBeenCalled();
        });

        test('should do nothing when payload.chunk is missing', () => {
            // Arrange
            const state = {
                appendToCurrentMessage: jest.fn()
            };

            const payload = {
                message: 'No chunk here'
            };

            // Act
            aiService.processStreamChunk(payload, state);

            // Assert
            expect(state.appendToCurrentMessage).not.toHaveBeenCalled();
        });
    });
});
