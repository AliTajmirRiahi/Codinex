/*
 * path: Codinex\UI\ToolWindows\Resources\Chat\js\views\addProviderView.js
 */
import { $, togglePanelHidden } from '../utils/dom.js';
import { validationService } from '../services/validationService.js';

const FIELD_TARGETS = [
    '#add-provider-name',
    '#add-provider-icon-pick-btn',
    '#add-provider-protocol',
    '#add-provider-base-url',
    '#add-provider-model-endpoint-url',
    '#add-provider-api-key',
];

// SVGs are tiny; this keeps the encoded icon small in storage and in webview messages.
const MAX_ICON_FILE_SIZE = 100 * 1024;

/**
 * Derives the relative ModelEndPoint (e.g. "/models") from the full model endpoint
 * URL the user typed, by stripping the Base URL prefix off of it.
 * e.g. baseUrl "https://api.openai.com/v1", modelEndpointUrl "https://api.openai.com/v1/models"
 * -> "/models". Returns null if the endpoint URL doesn't actually start with the base URL.
 */
function deriveModelEndPoint(baseUrl, modelEndpointUrl) {
    const base = (baseUrl || '').trim().replace(/\/+$/, '');
    const full = (modelEndpointUrl || '').trim().replace(/\/+$/, '');

    if (!base || !full) return null;
    if (full.toLowerCase() === base.toLowerCase()) return null;
    if (!full.toLowerCase().startsWith(base.toLowerCase())) return null;

    const relative = full.slice(base.length);
    if (!relative.startsWith('/')) return null;

    return relative;
}

/**
 * Normalizes a hex color string typed by the user (3 or 6 digits, "#" optional)
 * into a canonical "#rrggbb" form, or null if it isn't a valid hex color.
 */
function normalizeHexColor(value) {
    const match = /^#?([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$/.exec((value || '').trim());
    if (!match) return null;

    let hex = match[1];
    if (hex.length === 3) {
        hex = hex.split('').map(c => c + c).join('');
    }

    return `#${hex.toLowerCase()}`;
}

// Elements that actually paint a shape — missing fill defaults to black here, so we
// add fill="currentColor" when it's absent, not just when it's hardcoded to something else.
const SHAPE_TAGS = ['path', 'circle', 'ellipse', 'rect', 'line', 'polyline', 'polygon', 'text', 'use'];
// Wrapper elements — a fill here only matters if it was explicitly set (e.g. a color
// applied once on a <g> covering several children); we never force one onto them.
const CONTAINER_TAGS = ['g', 'svg'];

function readSvgFileAsText(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result);
        reader.onerror = () => reject(reader.error || new Error('Could not read the selected file.'));
        reader.readAsText(file);
    });
}

/**
 * Unicode-safe base64 encoding, since raw btoa() only accepts Latin1 bytes.
 */
function svgTextToDataUri(svgText) {
    const base64 = btoa(unescape(encodeURIComponent(svgText)));
    return `data:image/svg+xml;base64,${base64}`;
}

/**
 * True for a paint value that would block IconColor from having any effect: a solid
 * hardcoded color. False for "none" (intentionally unpainted), "currentColor" (already
 * tintable), and url(#...) references (gradients/patterns — not a plain color, leave alone).
 */
function isTintable(value) {
    if (!value) return false;
    const v = value.trim().toLowerCase();
    if (v === 'none' || v === 'currentcolor') return false;
    if (v.startsWith('url(')) return false;
    return true;
}

/**
 * Replaces a hardcoded fill/stroke/color value inside a CSS declaration block
 * (an inline style="" attribute, or the body of a <style> tag) with currentColor,
 * leaving "none" / currentColor / url() paint-server references untouched.
 */
function patchCssColors(css) {
    return css.replace(/(fill|stroke|color)\s*:\s*([^;}"']+)/gi,
        (match, prop, value) => isTintable(value) ? `${prop}:currentColor` : match);
}

function patchElementColor(el, isShape) {
    const fill = el.getAttribute('fill');
    if ((isShape && fill === null) || isTintable(fill)) {
        el.setAttribute('fill', 'currentColor');
    }

    if (isTintable(el.getAttribute('stroke'))) {
        el.setAttribute('stroke', 'currentColor');
    }

    const style = el.getAttribute('style');
    if (style) {
        el.setAttribute('style', patchCssColors(style));
    }
}

/**
 * Checks whether an SVG's shapes will actually pick up a CSS `color` (i.e. use
 * currentColor) and rewrites whatever won't — hardcoded fill/stroke attributes,
 * inline style="" colors, and colors set inside embedded <style> rules (those take
 * priority over presentation attributes, so they have to be patched too) — so the
 * IconColor picker always has a visible effect, not just for SVGs that already
 * happen to be authored with currentColor.
 */
function normalizeSvgColor(svgText) {
    const doc = new DOMParser().parseFromString(svgText, 'image/svg+xml');

    if (doc.querySelector('parsererror') || doc.documentElement.nodeName.toLowerCase() !== 'svg') {
        throw new Error('The selected file is not a valid SVG.');
    }

    SHAPE_TAGS.forEach(tag => {
        doc.querySelectorAll(tag).forEach(el => patchElementColor(el, true));
    });

    CONTAINER_TAGS.forEach(tag => {
        doc.querySelectorAll(tag).forEach(el => patchElementColor(el, false));
    });

    doc.querySelectorAll('style').forEach(styleEl => {
        styleEl.textContent = patchCssColors(styleEl.textContent || '');
    });

    return new XMLSerializer().serializeToString(doc);
}

/**
 * Reads a user-picked SVG file, auto-fixes it to accept a tint color if it doesn't
 * already, and returns it as a data URI (e.g. "data:image/svg+xml;base64,...").
 * The provider's icon is stored and shipped around as this data URI — there's no file
 * on disk for it, so it can't be resolved through the bundled-icon reference protocol
 * that <codinex-icon> normally uses; it has to be embedded directly instead.
 */
async function loadAndNormalizeSvgFile(file) {
    if (!file) throw new Error('No file selected.');

    const isSvg = file.type === 'image/svg+xml' || /\.svg$/i.test(file.name);
    if (!isSvg) throw new Error('Please choose an .svg file.');

    if (file.size > MAX_ICON_FILE_SIZE) throw new Error('Logo file is too large (max 100 KB).');

    const svgText = await readSvgFileAsText(file);
    const normalizedSvgText = normalizeSvgColor(svgText);

    return svgTextToDataUri(normalizedSvgText);
}

/**
 * Manages the "Add Custom Provider" modal: form state, validation,
 * and loading/error presentation while the provider is being verified.
 */
export const addProviderView = {
    _iconDataUri: '',
    _editingProviderId: null,

    initEventHandlers(saveCallBack) {
        const needApiKeyCheckbox = $('#add-provider-need-api-key');
        if (needApiKeyCheckbox) {
            needApiKeyCheckbox.addEventListener('change', () => {
                togglePanelHidden('#add-provider-api-key-group', needApiKeyCheckbox.checked);
            });
        }

        const iconFileInput = $('#add-provider-icon-file');
        const iconPickBtn = $('#add-provider-icon-pick-btn');
        const iconClearBtn = $('#add-provider-icon-clear-btn');
        const iconColorInput = $('#add-provider-icon-color');
        const iconColorHexInput = $('#add-provider-icon-color-hex');

        if (iconPickBtn && iconFileInput) {
            iconPickBtn.addEventListener('click', () => iconFileInput.click());
        }

        if (iconFileInput) {
            iconFileInput.addEventListener('change', async () => {
                const file = iconFileInput.files && iconFileInput.files[0];
                iconFileInput.value = '';
                if (!file) return;

                validationService.clearInlineError('#add-provider-icon-pick-btn');

                try {
                    this._iconDataUri = await loadAndNormalizeSvgFile(file);
                    this._updateIconPreview();
                } catch (err) {
                    validationService.showError({
                        message: err.message || 'Could not load the selected logo.',
                        mode: 'inline',
                        target: '#add-provider-icon-pick-btn'
                    });
                }
            });
        }

        if (iconClearBtn) {
            iconClearBtn.addEventListener('click', () => {
                this._iconDataUri = '';
                this._updateIconPreview();
            });
        }

        if (iconColorInput) {
            iconColorInput.addEventListener('input', () => {
                if (iconColorHexInput) iconColorHexInput.value = iconColorInput.value;
                this._updateIconPreview();
            });
        }

        if (iconColorHexInput) {
            // Only commit while the user is typing once the value is a complete, valid hex
            // color — an in-progress value like "#3f" is left alone rather than rejected.
            iconColorHexInput.addEventListener('input', () => {
                const normalized = normalizeHexColor(iconColorHexInput.value);
                if (!normalized) return;

                if (iconColorInput) iconColorInput.value = normalized;
                this._updateIconPreview();
            });

            iconColorHexInput.addEventListener('blur', () => {
                const normalized = normalizeHexColor(iconColorHexInput.value)
                    || (iconColorInput ? iconColorInput.value : '#000000');

                iconColorHexInput.value = normalized;
                if (iconColorInput) iconColorInput.value = normalized;
                this._updateIconPreview();
            });
        }

        $('#save-add-provider-btn').onclick = () => {
            const raw = this.getFormData();

            const rules = [
                { field: 'name', validator: validationService.isNotEmpty, message: 'Provider name is required.', mode: 'inline', target: '#add-provider-name' },
                { field: 'protocol', validator: validationService.isSelected, message: 'Please select a protocol.', mode: 'inline', target: '#add-provider-protocol' },
                { field: 'baseUrl', validator: validationService.isNotEmpty, message: 'Base URL is required.', mode: 'inline', target: '#add-provider-base-url' },
                {
                    field: 'modelEndpointUrl',
                    validator: (value) => !!deriveModelEndPoint(raw.baseUrl, value),
                    message: 'Model Endpoint must be the full URL and must start with the Base URL above.',
                    mode: 'inline',
                    target: '#add-provider-model-endpoint-url'
                },
                {
                    field: 'iconColor',
                    validator: (value) => !!normalizeHexColor(value),
                    message: 'Icon color must be a valid hex color (e.g. #000000).',
                    mode: 'inline',
                    target: '#add-provider-icon-color-hex'
                },
            ];

            if (raw.needApiKey) {
                rules.push({ field: 'apiKey', validator: validationService.isNotEmpty, message: 'API key is required.', mode: 'inline', target: '#add-provider-api-key' });
            }

            const validation = validationService.validate(raw, { rules });

            if (!validation.valid) {
                validationService.showErrors(validation.errors);
                return;
            }

            const data = {
                name: raw.name,
                icon: raw.icon,
                iconColor: normalizeHexColor(raw.iconColor) || '#000000',
                protocol: raw.protocol,
                baseUrl: raw.baseUrl,
                modelEndPoint: deriveModelEndPoint(raw.baseUrl, raw.modelEndpointUrl),
                needApiKey: raw.needApiKey,
                apiKey: raw.apiKey,
            };

            this.clearErrors();
            this.setLoading(true);

            if (saveCallBack) saveCallBack(data, this._editingProviderId);
        };
    },

    getFormData() {
        return {
            name: $('#add-provider-name').value.trim(),
            icon: this._iconDataUri || '',
            iconColor: $('#add-provider-icon-color-hex').value || $('#add-provider-icon-color').value || '#000000',
            protocol: $('#add-provider-protocol').value,
            baseUrl: $('#add-provider-base-url').value.trim(),
            modelEndpointUrl: $('#add-provider-model-endpoint-url').value.trim(),
            needApiKey: $('#add-provider-need-api-key').checked,
            apiKey: $('#add-provider-api-key').value.trim(),
        };
    },

    clearErrors() {
        FIELD_TARGETS.forEach(target => validationService.clearInlineError(target));
    },

    resetForm() {
        $('#add-provider-name').value = '';
        $('#add-provider-protocol').value = 'openai';
        $('#add-provider-base-url').value = '';
        $('#add-provider-model-endpoint-url').value = '';
        $('#add-provider-need-api-key').checked = true;
        $('#add-provider-api-key').value = '';
        togglePanelHidden('#add-provider-api-key-group', true);

        this._iconDataUri = '';
        this._setIconColor('#000000');
        this._updateIconPreview();

        this._editingProviderId = null;
        $('#add-provider-modal-title').textContent = 'Add Custom Provider';
        $('#save-add-provider-btn').textContent = 'Save Provider';

        this.clearErrors();
    },

    /**
     * @private
     */
    _updateIconPreview() {
        const preview = $('#add-provider-icon-preview');
        if (preview) {
            preview.setAttribute('name', this._iconDataUri || 'puzzle');
            preview.style.color = $('#add-provider-icon-color')?.value || '#000000';
        }

        togglePanelHidden('#add-provider-icon-clear-btn', !!this._iconDataUri);
    },

    /**
     * @private
     */
    _setIconColor(color) {
        const normalized = normalizeHexColor(color) || '#000000';
        const swatch = $('#add-provider-icon-color');
        const hex = $('#add-provider-icon-color-hex');

        if (swatch) swatch.value = normalized;
        if (hex) hex.value = normalized;
    },

    setLoading(isLoading) {
        togglePanelHidden('#add-provider-loading-screen', !!isLoading);
    },

    showError(message) {
        this.setLoading(false);

        validationService.showError({
            message: message || 'Provider could not be added.',
            mode: 'inline',
            target: '#add-provider-model-endpoint-url'
        });
    },

    /**
     * Shows the modal in "Add" mode.
     */
    show() {
        this.resetForm();
        togglePanelHidden('#add-provider-modal', true);
    },

    /**
     * Shows the modal pre-filled with an existing custom provider's data, in "Edit" mode.
     * Save then sends an update for this provider instead of creating a new one.
     */
    showForEdit(provider) {
        this.resetForm();

        this._editingProviderId = provider.id || provider.Id || null;
        $('#add-provider-modal-title').textContent = 'Edit Custom Provider';
        $('#save-add-provider-btn').textContent = 'Save Changes';

        $('#add-provider-name').value = provider.name || provider.Name || '';
        $('#add-provider-protocol').value = provider.protocol || provider.Protocol || 'openai';

        const baseUrl = provider.baseUrl || provider.BaseUrl || '';
        const modelEndPoint = provider.modelEndPoint || provider.ModelEndPoint || '';
        $('#add-provider-base-url').value = baseUrl;
        $('#add-provider-model-endpoint-url').value = modelEndPoint ? `${baseUrl}${modelEndPoint}` : '';

        const needApiKey = provider.needApiKey !== false && provider.NeedApiKey !== false;
        $('#add-provider-need-api-key').checked = needApiKey;
        togglePanelHidden('#add-provider-api-key-group', needApiKey);
        $('#add-provider-api-key').value = provider.apiKey || provider.ApiKey || '';

        // Bundled default icon names (e.g. "puzzle") aren't editable logos — only a
        // previously embedded data URI counts as a custom logo the user can replace/remove.
        // The preview still shows whatever icon (bundled or embedded) the provider has today.
        const icon = provider.icon || provider.Icon || '';
        this._iconDataUri = icon.startsWith('data:') ? icon : '';
        this._setIconColor(provider.iconColor || provider.IconColor || '#000000');

        const preview = $('#add-provider-icon-preview');
        if (preview) {
            preview.setAttribute('name', icon || 'puzzle');
            preview.style.color = $('#add-provider-icon-color').value;
        }
        togglePanelHidden('#add-provider-icon-clear-btn', !!this._iconDataUri);

        togglePanelHidden('#add-provider-modal', true);
    },

    /**
     * Hides the Add Custom Provider modal. Only called after a successful save
     * (or an explicit cancel) — never automatically while a save is pending.
     */
    hide() {
        this.setLoading(false);
        togglePanelHidden('#add-provider-modal', false);
    },
};
