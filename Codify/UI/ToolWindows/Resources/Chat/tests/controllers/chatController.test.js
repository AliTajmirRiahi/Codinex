/**
 * @file chatController.test.js
 * Tests for initChatController in chatController.js
 */

// Mock dependencies before importing the module under test
jest.mock('../../js/views/chatView.js', () => ({
    appendMessage: jest.fn(),
    createStreamingMessage: jest.fn(),
}));

jest.mock('../../js/services/aiService.js', () => ({
    aiService: {
        sendMessage: jest.fn(),
    },
}));

jest.mock('../../js/state/appState.js', () => ({
    getState: jest.fn(),
    setState: jest.fn(),
}));

import { initChatController } from '../../js/controllers/chatController.js';
import { appendMessage, createStreamingMessage } from '../../js/views/chatView.js';
import { aiService } from '../../js/services/aiService.js';
import { getState, setState } from '../../js/state/appState.js';

describe('initChatController', () => {
    let userInput;
    let sendBtn;
    let bridge;

    beforeEach(() => {
        // Reset mocks before each test
        jest.clearAllMocks();

        // Mock bridge object passed to aiService
        bridge = { name: 'test-bridge' };

        // Create DOM elements used by the controller
        userInput = document.createElement('textarea');
        userInput.id = 'userInput';

        sendBtn = document.createElement('button');
        sendBtn.id = 'sendBtn';

        document.body.innerHTML = '';
        document.body.appendChild(userInput);
        document.body.appendChild(sendBtn);
    });

    test('should warn and return if input or button is missing', () => {
        // Remove DOM elements
        document.body.innerHTML = '';

        const warnSpy = jest.spyOn(console, 'warn').mockImplementation(() => { });

        initChatController(bridge);

        expect(warnSpy).toHaveBeenCalledWith('Chat input or send button not found');

        warnSpy.mockRestore();
    });

    test('should do nothing when input is empty', () => {
        userInput.value = '   ';

        initChatController(bridge);

        sendBtn.click();

        expect(appendMessage).not.toHaveBeenCalled();
        expect(aiService.sendMessage).not.toHaveBeenCalled();
        expect(setState).not.toHaveBeenCalled();
    });

    test('should do nothing when app is loading', () => {
        userInput.value = 'Hello';
        getState.mockReturnValue({ isLoading: true });

        initChatController(bridge);

        sendBtn.click();

        expect(appendMessage).not.toHaveBeenCalled();
        expect(aiService.sendMessage).not.toHaveBeenCalled();
        expect(setState).not.toHaveBeenCalled();
    });

    test('should send message and update UI on success', async () => {
        userInput.value = 'Hello AI';

        getState.mockReturnValue({ isLoading: false });
        aiService.sendMessage.mockResolvedValue('AI response');
        createStreamingMessage.mockReturnValue({
            innerText: '',
        });

        initChatController(bridge);

        await sendBtn.click();

        expect(userInput.value).toBe('');
        expect(appendMessage).toHaveBeenCalledWith('Hello AI', 'user');
        expect(setState).toHaveBeenCalledWith({ isLoading: true });
        expect(aiService.sendMessage).toHaveBeenCalledWith('Hello AI', bridge);
        expect(createStreamingMessage).toHaveBeenCalled();
        expect(setState).toHaveBeenCalledWith({ isLoading: false });
    });

    test('should handle aiService error and show assistant error message', async () => {
        userInput.value = 'Hello AI';

        getState.mockReturnValue({ isLoading: false });
        aiService.sendMessage.mockRejectedValue(new Error('Network error'));
        createStreamingMessage.mockReturnValue({
            innerText: '',
        });

        const errorSpy = jest.spyOn(console, 'error').mockImplementation(() => { });

        initChatController(bridge);

        await sendBtn.click();

        expect(errorSpy).toHaveBeenCalled();
        expect(appendMessage).toHaveBeenCalledWith(
            '⚠️ Error generating response.',
            'assistant'
        );
        expect(setState).toHaveBeenCalledWith({ isLoading: false });

        errorSpy.mockRestore();
    });

    test('should send on Enter without Shift', () => {
        userInput.value = 'Hello Enter';
        getState.mockReturnValue({ isLoading: false });
        aiService.sendMessage.mockResolvedValue('AI response');
        createStreamingMessage.mockReturnValue({
            innerText: '',
        });

        initChatController(bridge);

        const event = new KeyboardEvent('keydown', {
            key: 'Enter',
            shiftKey: false,
            bubbles: true,
            cancelable: true,
        });

        userInput.dispatchEvent(event);

        expect(appendMessage).toHaveBeenCalledWith('Hello Enter', 'user');
        expect(aiService.sendMessage).toHaveBeenCalledWith('Hello Enter', bridge);
    });

    test('should not send on Shift+Enter', () => {
        userInput.value = 'Hello\nWorld';
        getState.mockReturnValue({ isLoading: false });

        initChatController(bridge);

        const event = new KeyboardEvent('keydown', {
            key: 'Enter',
            shiftKey: true,
            bubbles: true,
            cancelable: true,
        });

        userInput.dispatchEvent(event);

        expect(appendMessage).not.toHaveBeenCalled();
        expect(aiService.sendMessage).not.toHaveBeenCalled();
    });
});
