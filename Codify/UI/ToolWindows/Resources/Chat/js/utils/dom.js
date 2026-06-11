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