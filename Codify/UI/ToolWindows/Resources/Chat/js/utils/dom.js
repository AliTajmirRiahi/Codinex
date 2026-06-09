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
