/**
 * @file validationService.test.js
 * Unit tests for validationService
 */

import { validationService } from '../../js/services/validationService.js';

describe('validationService', () => {

    describe('isValidUrl', () => {
        test('should return true for valid HTTP/HTTPS URLs', () => {
            expect(validationService.isValidUrl('https://api.openai.com')).toBe(true);
            expect(validationService.isValidUrl('http://localhost:11434')).toBe(true); // Local Ollama port
        });

        test('should return false for malformed URLs', () => {
            expect(validationService.isValidUrl('not-a-url')).toBe(false);
            expect(validationService.isValidUrl('http//missing-colon')).toBe(false);
            expect(validationService.isValidUrl('')).toBe(false);
        });

        test('should return false for null or undefined', () => {
            expect(validationService.isValidUrl(null)).toBe(false);
            expect(validationService.isValidUrl(undefined)).toBe(false);
        });
    });

    describe('isNotEmpty', () => {
        test('should return true for non-empty strings', () => {
            expect(validationService.isNotEmpty('Hello')).toBe(true);
            expect(validationService.isNotEmpty(' a ')).toBe(true);
        });

        test('should return false for empty or whitespace-only strings', () => {
            expect(validationService.isNotEmpty('')).toBe(false);
            expect(validationService.isNotEmpty('   ')).toBe(false);
            expect(validationService.isNotEmpty('\n\t')).toBe(false);
        });

        test('should return false for null or undefined', () => {
            expect(validationService.isNotEmpty(null)).toBe(false);
            expect(validationService.isNotEmpty(undefined)).toBe(false);
        });
    });
});
