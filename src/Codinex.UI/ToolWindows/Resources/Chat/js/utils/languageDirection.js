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
