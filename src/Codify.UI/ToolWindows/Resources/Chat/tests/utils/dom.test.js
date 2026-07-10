/**
 * @file dom.test.js
 * Unit tests for DOM utility helpers
 */

import { $, $$, createElement } from '../../js/utils/dom.js';

describe('dom utilities', () => {
    beforeEach(() => {
        // Reset DOM before each test
        document.body.innerHTML = '';
    });

    test('should return the first matching element with $', () => {
        document.body.innerHTML = `
            <div class="item">First</div>
            <div class="item">Second</div>
        `;

        const el = $('.item');

        expect(el).not.toBeNull();
        expect(el.textContent).toBe('First');
    });

    test('should return all matching elements with $$', () => {
        document.body.innerHTML = `
            <div class="item">First</div>
            <div class="item">Second</div>
        `;

        const elements = $$('.item');

        expect(elements.length).toBe(2);
        expect(elements[0].textContent).toBe('First');
        expect(elements[1].textContent).toBe('Second');
    });

    test('should create element with tag, className and innerHTML', () => {
        const el = createElement('div', 'message-item user', '<span>Hello</span>');

        expect(el.tagName).toBe('DIV');
        expect(el.className).toBe('message-item user');
        expect(el.innerHTML).toBe('<span>Hello</span>');
    });

    test('should create element without className', () => {
        const el = createElement('span', '', 'Hello');

        expect(el.tagName).toBe('SPAN');
        expect(el.className).toBe('');
        expect(el.innerHTML).toBe('Hello');
    });

    test('should create element without innerHTML', () => {
        const el = createElement('p', 'text-content');

        expect(el.tagName).toBe('P');
        expect(el.className).toBe('text-content');
        expect(el.innerHTML).toBe('');
    });
});
