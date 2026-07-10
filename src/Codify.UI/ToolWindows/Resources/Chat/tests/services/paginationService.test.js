/**
 * @file paginationService.test.js
 * Unit tests for paginationService
 */

import { paginationService } from '../../js/services/paginationService.js';

describe('paginationService', () => {

    beforeEach(() => {
        // Reset state before every test to avoid leakage
        paginationService.currentPage = 0;
        paginationService.pageSize = 5;
    });

    test('should return first page of messages', () => {
        const messages = Array.from({ length: 12 }, (_, i) => `msg-${i}`);

        const result = paginationService.getPaginated(messages);

        expect(result).toEqual([
            'msg-0',
            'msg-1',
            'msg-2',
            'msg-3',
            'msg-4'
        ]);
    });

    test('should return next page after calling nextPage()', () => {
        const messages = Array.from({ length: 12 }, (_, i) => `msg-${i}`);

        paginationService.nextPage();
        const result = paginationService.getPaginated(messages);

        expect(result).toEqual([
            'msg-5',
            'msg-6',
            'msg-7',
            'msg-8',
            'msg-9'
        ]);
    });

    test('should return remaining messages if last page is smaller than pageSize', () => {
        const messages = Array.from({ length: 12 }, (_, i) => `msg-${i}`);

        paginationService.nextPage(); // page 1
        paginationService.nextPage(); // page 2

        const result = paginationService.getPaginated(messages);

        expect(result).toEqual([
            'msg-10',
            'msg-11'
        ]);
    });

    test('should return empty array if page exceeds available messages', () => {
        const messages = Array.from({ length: 5 }, (_, i) => `msg-${i}`);

        paginationService.nextPage(); // page 1 (beyond range)

        const result = paginationService.getPaginated(messages);

        expect(result).toEqual([]);
    });

});
