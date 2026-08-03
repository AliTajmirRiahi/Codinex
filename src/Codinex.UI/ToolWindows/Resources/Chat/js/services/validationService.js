/**
 * Validation service for settings form.
 * Supports dynamic rules and multiple error display modes.
 */

export const validationService = {
    validate(data, options = {}) {
        const errors = [];

        const rules = options.rules || [
            { field: 'provider', validator: this.isSelected, message: 'Please select a provider.' },
            { field: 'models', validator: this.hasSelectedItems, message: 'Please select at least one model.' },
            { field: 'apiKey', validator: this.isNotEmpty, message: 'Please enter your API key.' },
        ];

        for (const rule of rules) {
            const value = data?.[rule.field];
            const isValid = rule.validator(value, data);

            if (!isValid) {
                errors.push({
                    field: rule.field,
                    message: rule.message || 'Invalid value.',
                    mode: rule.mode || options.defaultMode || 'inline',
                    target: rule.target || null,
                });
            }
        }

        return {
            valid: errors.length === 0,
            errors,
        };
    },

    isSelected(value) {
        return !!value;
    },

    isNotEmpty(value) {
        return !!(value && String(value).trim().length > 0);
    },

    hasSelectedItems(value) {
        if (Array.isArray(value)) return value.length > 0;
        if (value instanceof Map) return value.size > 0;
        if (value && typeof value === 'object') return Object.keys(value).length > 0;
        return false;
    },

    showErrors(errors = []) {
        for (const error of errors) {
            this.showError(error);
        }
    },

    showError(error) {
        if (!error) return;

        switch (error.mode) {
            case 'popup':
                this._showPopup(error.message);
                break;
            case 'toast':
                this._showToast(error.message);
                break;
            case 'inline':
            default:
                this._showInline(error.target, error.message);
                break;
        }
    },

    _showPopup(message) {
        alert(message);
    },

    _showToast(message) {
        Toastify({
            text: message,
            duration: 4000,
            gravity: 'top',
            position: 'center',
            close: true,
            style: {
                background: 'var(--vs-class-designer-unresolved-text-color)'
            }
        }).showToast();
    },

    _showInline(target, message) {
        if (!target) return;

        const element = typeof target === 'string' ? document.querySelector(target) : target;
        if (!element) return;

        let errorEl = element.nextElementSibling;

        if (!errorEl || !errorEl.classList.contains('validation-error')) {
            errorEl = document.createElement('div');
            errorEl.className = 'validation-error';
            errorEl.style.color = '#ff6b6b';
            errorEl.style.fontSize = '12px';
            errorEl.style.marginTop = '4px';
            element.insertAdjacentElement('afterend', errorEl);
        }

        errorEl.textContent = message;
    },

    clearInlineError(target) {
        const element = typeof target === 'string' ? document.querySelector(target) : target;
        if (!element) return;

        const errorEl = element.nextElementSibling;
        if (errorEl && errorEl.classList.contains('validation-error')) {
            errorEl.remove();
        }
    },
};
