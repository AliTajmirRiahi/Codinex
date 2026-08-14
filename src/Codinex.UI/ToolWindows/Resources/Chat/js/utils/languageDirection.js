/**
 * languageDirection.js
 * Flips the composer input direction (RTL/LTR) to match the active
 * Windows keyboard input language (e.g. Farsi/Arabic -> RTL, English -> LTR).
 */
import { $ } from './dom.js';

/**
 * Applies the given direction to the chat composer input.
 * @param {boolean} isRightToLeft - Whether the active input language reads right-to-left.
 */
export function applyComposerDirection(isRightToLeft) {
    const direction = isRightToLeft ? 'rtl' : 'ltr';

    const userInput = $('#userInput');
    if (userInput) {
        userInput.dir = direction;
        userInput.classList.toggle('rtl', isRightToLeft);
    }
}

// Unicode code-point ranges for right-to-left scripts (Hebrew, Arabic,
// Persian/Farsi, Urdu, and their presentation-form variants). Expressed as
// numeric ranges rather than a literal-character regex so the codepoints are
// unambiguous regardless of source-file encoding.
const RTL_RANGES = [
    [0x0590, 0x05FF], // Hebrew
    [0x0600, 0x06FF], // Arabic (covers Persian/Farsi, Urdu base letters)
    [0x0750, 0x077F], // Arabic Supplement
    [0x08A0, 0x08FF], // Arabic Extended-A
    [0xFB1D, 0xFDFF], // Hebrew / Arabic Presentation Forms-A
    [0xFE70, 0xFEFF]  // Arabic Presentation Forms-B
];

/**
 * Detects whether a piece of text (e.g. AI-generated content) reads right-to-left,
 * based on the presence of RTL-script characters — independent of the OS keyboard
 * input language, since this text isn't typed by the user.
 * @param {string} text
 * @returns {boolean}
 */
export function isRtlText(text) {
    if (!text) return false;

    for (const char of text) {
        const code = char.codePointAt(0);

        if (RTL_RANGES.some(([start, end]) => code >= start && code <= end)) {
            return true;
        }
    }

    return false;
}
