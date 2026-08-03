/**
 * @file debounce.test.js
 * Unit tests for debounce utility using Jest fake timers
 */

import { debounce } from '../../js/utils/debounce.js';

describe('debounce utility', () => {
    // Enable fake timers before tests
    beforeEach(() => {
        jest.useFakeTimers();
    });

    // Restore real timers after tests
    afterEach(() => {
        jest.useRealTimers();
    });

    test('should delay function execution', () => {
        const func = jest.fn();
        const debouncedFunc = debounce(func, 1000);

        debouncedFunc();

        // Use toHaveBeenCalled instead of toBeCalled
        expect(func).not.toHaveBeenCalled();

        // Fast-forward time by 500ms
        jest.advanceTimersByTime(500);
        expect(func).not.toHaveBeenCalled();

        // Fast-forward time to 1000ms
        jest.advanceTimersByTime(500);
        expect(func).toHaveBeenCalled();
        expect(func).toHaveBeenCalledTimes(1);
    });

    test('should only execute once if called multiple times rapidly', () => {
        const func = jest.fn();
        const debouncedFunc = debounce(func, 1000);

        debouncedFunc();
        debouncedFunc();
        debouncedFunc();

        jest.runAllTimers();

        expect(func).toHaveBeenCalledTimes(1);
    });

    test('should pass correct arguments to the original function', () => {
        const func = jest.fn();
        const debouncedFunc = debounce(func, 1000);

        debouncedFunc('test-arg', 123);
        jest.runAllTimers();

        expect(func).toHaveBeenCalledWith('test-arg', 123);
    });

    test('should reset timer if called again within wait period', () => {
        const func = jest.fn();
        const debouncedFunc = debounce(func, 1000);

        debouncedFunc();

        jest.advanceTimersByTime(800);

        debouncedFunc();

        jest.advanceTimersByTime(800);
        expect(func).not.toHaveBeenCalled(); // Changed here too

        jest.advanceTimersByTime(200);
        expect(func).toHaveBeenCalledTimes(1);
    });
});
