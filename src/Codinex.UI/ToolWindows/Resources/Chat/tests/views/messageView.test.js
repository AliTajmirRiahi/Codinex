/**
 * @file messageView.test.js
 * Unit tests for message rendering logic in messageView
 */

import { messageView } from '../../js/views/messageView.js';
import { createElement } from '../../js/utils/dom.js';

// 1. Mock the module without using 'document' inside the factory
jest.mock('../../js/utils/dom.js', () => ({
    createElement: jest.fn()
}));

describe('messageView', () => {
    beforeEach(() => {
        jest.clearAllMocks();

        // 2. Define the implementation here, inside the test context where 'document' is available
        createElement.mockImplementation((tag, className, content) => {
            const el = document.createElement(tag);
            if (className) el.className = className;
            if (content) el.innerHTML = content;
            return el;
        });
    });

    test('should create a message element with correct structure for "user" role', () => {
        const role = 'user';
        const content = 'Hello AI!';

        const element = messageView.createMessageElement(role, content);

        // Assertions
        expect(element.classList.contains('message-item')).toBe(true);
        expect(element.classList.contains('user')).toBe(true);
        expect(createElement).toHaveBeenCalledWith('div', `message-item ${role}`);

        const avatar = element.querySelector('.avatar');
        expect(avatar).not.toBeNull();
        expect(avatar.innerHTML).toContain('name="user-avatar"');

        const contentDiv = element.querySelector('.content');
        expect(contentDiv.innerHTML).toBe(content);
    });

    test('should create a message element with "assistant" role', () => {
        const role = 'assistant';
        const content = 'Response...';

        const element = messageView.createMessageElement(role, content);

        expect(element.classList.contains('assistant')).toBe(true);
        const icon = element.querySelector('codify-icon');
        expect(icon.getAttribute('name')).toBe('assistant-avatar');
    });

    test('should have exactly two children: avatar and content', () => {
        const element = messageView.createMessageElement('user', 'test');

        expect(element.children.length).toBe(2);
        expect(element.children[0].classList.contains('avatar')).toBe(true);
        expect(element.children[1].classList.contains('content')).toBe(true);
    });
});
