/**
 * DOM Utility helpers for cleaner syntax.
 */
export const $ = (selector) => document.querySelector(selector);
export const $$ = (selector) => document.querySelectorAll(selector);

export const createElement = (tag, className, innerHTML) => {
    const el = document.createElement(tag);
    if (className) el.className = className;
    if (innerHTML) el.innerHTML = innerHTML;
    return el;
};

export const addDefaultOption = (select, text = 'Select an option') => {
    // Add a default/placeholder option
    const defaultOption = document.createElement('option');
    defaultOption.value = "";
    defaultOption.textContent = text;
    defaultOption.disabled = true;
    defaultOption.selected = true;
    select.appendChild(defaultOption);
}

export const togglePanelHidden = (element, visible) => {
    const modal = $(element);
    if (modal) {
        modal.classList.toggle('hidden', !visible);
    }
}
export const togglePanelDisable = (element, visible) => {
    const modal = $(element);
    if (modal) {
        modal.classList.toggle('disable', !visible);
    }
}

export const trigger = (element ,event) => {
    element.dispatchEvent(new Event('change', { bubbles: true }));
}

/**
 * Converts a date string to a relative time string (e.g., Today, Yesterday, 2 days ago)
 * @param {string} dateString - ISO Date string
 * @returns {string} Relative time
 */
export const getRelativeDate = (dateString) => {
    const date = new Date(dateString);
    const now = new Date();

    // Reset hours to compare only days
    const diffTime = now.setHours(0, 0, 0, 0) - date.setHours(0, 0, 0, 0);
    const diffDays = Math.floor(diffTime / (1000 * 60 * 60 * 24));

    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return 'Yesterday';
    if (diffDays < 7) return `${diffDays} days ago`;

    // For older dates, return short date format
    return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
}

//window.copyToClipboard = (btn) => {
//    // Find the code element relative to the button
//    const codeElement = btn.closest('.code-wrapper').querySelector('code');
//    const text = codeElement.innerText;

//    navigator.clipboard.writeText(text).then(() => {
//        const originalText = btn.innerText;
//        btn.innerText = 'Copied!';
//        btn.style.color = '#4ec9b0'; // VS Code Green
//        setTimeout(() => {
//            btn.innerText = originalText;
//            btn.style.color = '';
//        }, 2000);
//    });
//};