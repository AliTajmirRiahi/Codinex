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

const COLORABLE_TAGS = ['path', 'circle', 'ellipse', 'rect', 'line', 'polyline', 'polygon'];

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

function isTintable(value) {
    if (!value) return false;
    const v = value.trim().toLowerCase();
    return v !== 'none' && v !== 'currentcolor';
}

/**
 * Replaces a hardcoded fill/stroke value inside an inline style="" declaration
 * with currentColor, leaving "none" / already-tintable values untouched.
 */
function patchStyleColors(style) {
    return style
        .replace(/(fill)\s*:\s*([^;]+)/gi, (match, prop, value) => isTintable(value) ? `${prop}:currentColor` : match)
        .replace(/(stroke)\s*:\s*([^;]+)/gi, (match, prop, value) => isTintable(value) ? `${prop}:currentColor` : match);
}

/**
 * Checks whether an SVG's shapes will actually pick up a CSS `color` (i.e. use
 * currentColor), and rewrites any hardcoded fill/stroke to currentColor when they
 * won't — so the IconColor picker always has a visible effect, not just for SVGs
 * that already happen to be authored with currentColor.
 */
function normalizeSvgColor(svgText) {
    const doc = new DOMParser().parseFromString(svgText, 'image/svg+xml');

    if (doc.querySelector('parsererror') || doc.documentElement.nodeName.toLowerCase() !== 'svg') {
        throw new Error('The selected file is not a valid SVG.');
    }

    COLORABLE_TAGS.forEach(tag => {
        doc.querySelectorAll(tag).forEach(el => {
            const fill = el.getAttribute('fill');
            if (fill === null || isTintable(fill)) {
                el.setAttribute('fill', 'currentColor');
            }

            const stroke = el.getAttribute('stroke');
            if (isTintable(stroke)) {
                el.setAttribute('stroke', 'currentColor');
            }

            const style = el.getAttribute('style');
            if (style) {
                el.setAttribute('style', patchStyleColors(style));
            }
        });
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
            iconColorInput.addEventListener('input', () => this._updateIconPreview());
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
                iconColor: raw.iconColor,
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
            iconColor: $('#add-provider-icon-color').value || '#000000',
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
        $('#add-provider-icon-color').value = '#000000';
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
        $('#add-provider-icon-color').value = provider.iconColor || provider.IconColor || '#000000';

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
