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